using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum TurnState { PlayerTurn, EnemyTurn, Victory, Defeat }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    // ===== Inspector =====
    [Header("Managers")]
    [SerializeField] private HexGridManager grid;
    [SerializeField] private HexSelector selector;

    [Header("Units (manually placed)")]
    [SerializeField] private List<Unit> playerUnits = new();
    [SerializeField] private List<Unit> enemyUnits = new();

    [Header("AI Timing")]
    [SerializeField] private float enemyStartDelay = 0.30f;
    [SerializeField] private float enemyStepDelay = 0.25f;

    [Header("End Turn UI")]
    [SerializeField] private Button nextTurnButton;
    [SerializeField] private ConfirmationPopup confirmPopup;
    [SerializeField] private bool promptIfUnitsCanStillMove = true;
    [SerializeField] private bool checkMovementInsteadOfActions = true;

    [Header("HUD")]
    [SerializeField] private TMP_Text turnLabel;
    [SerializeField] private TMP_Text phaseLabel;
    [SerializeField] private TMP_Text unitsLabel;

    [Header("End Game Events")]
    [SerializeField] private UnityEvent onVictory;
    [SerializeField] private UnityEvent onDefeat;
    [SerializeField] private bool stopPlayerInputOnGameEnd = true;

    // ===== C# events =====
    public event Action<int> OnTurnChanged;
    public event Action<TurnState> OnStateChanged;
    public event Action<int, int> OnUnitCountChanged;

    // ===== Public state =====
    public int TurnNumber { get; private set; }
    public TurnState CurrentState { get; private set; }
    public bool IsPlayerTurn => CurrentState == TurnState.PlayerTurn;

    // ===== Internal =====
    private bool nextTurnRequested;
    private bool popupOpen;
    private int lastCanAct = -1;
    private int lastAlive = -1;

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (nextTurnButton != null && nextTurnButton.onClick.GetPersistentEventCount() == 0)
            nextTurnButton.onClick.AddListener(NextTurnButtonPressed);
    }

    private void Start()
    {
        if (grid == null) Debug.LogError("[TurnManager] 'grid' is not assigned!");

        // Register any units manually placed in the scene with the grid.
        foreach (var u in playerUnits)
            if (u != null && grid != null)
                grid.RegisterUnit(u, grid.WorldToCell(u.transform.position));
        foreach (var u in enemyUnits)
            if (u != null && grid != null)
                grid.RegisterUnit(u, grid.WorldToCell(u.transform.position));

        // Pick up any units that were spawned by other scripts before we started.
        SyncUnitListsWithGrid();

        SetNextTurnButtonInteractable(false);
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        if (CurrentState == TurnState.PlayerTurn && !popupOpen)
        {
            // Catch units spawned mid-turn (SpawnTest, summons, etc.)
            SyncUnitListsWithGrid();
            RefreshUnitsUI();
        }
    }

    // ==================================================================
    //  UNIT REGISTRY — makes spawning "just work"
    // ==================================================================

    /// <summary>Add a runtime-spawned unit to the correct list. Called by SpawnTest / FightNodeController.</summary>
    public void RegisterSpawnedUnit(Unit unit)
    {
        if (unit == null) return;

        if (unit.Faction == Faction.Player)
        {
            if (!playerUnits.Contains(unit)) playerUnits.Add(unit);
        }
        else
        {
            if (!enemyUnits.Contains(unit)) enemyUnits.Add(unit);
        }

        RefreshUnitsUI();
    }

    /// <summary>Scan the grid and add any units that aren't in our lists yet.
    /// Also purges destroyed (Unity-null) entries.</summary>
    public void SyncUnitListsWithGrid()
    {
        if (grid == null) return;

        playerUnits.RemoveAll(u => u == null);
        enemyUnits.RemoveAll(u => u == null);

        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);
            if (u == null || !u.IsAlive) continue;

            if (u.Faction == Faction.Player)
            {
                if (!playerUnits.Contains(u)) playerUnits.Add(u);
            }
            else
            {
                if (!enemyUnits.Contains(u)) enemyUnits.Add(u);
            }
        }
    }

    // ==================================================================
    //  PUBLIC BUTTON HANDLERS
    // ==================================================================

    public void NextTurnButtonPressed()
    {
        if (CurrentState != TurnState.PlayerTurn) return;
        if (nextTurnRequested || popupOpen) return;

        int pending = CountStillActionablePlayers();

        if (promptIfUnitsCanStillMove && pending > 0 && confirmPopup != null)
        {
            popupOpen = true;
            string verb = checkMovementInsteadOfActions ? "move" : "act";
            string msg = pending == 1
                ? $"1 unit can still {verb}. End turn anyway?"
                : $"{pending} units can still {verb}. End turn anyway?";

            confirmPopup.Show(msg, ConfirmEndTurn, CancelEndTurn);
        }
        else
        {
            ConfirmEndTurn();
        }
    }

    public void ConfirmEndTurn() { popupOpen = false; nextTurnRequested = true; }
    public void CancelEndTurn() { popupOpen = false; }

    // ==================================================================
    //  MAIN LOOP
    // ==================================================================

    private IEnumerator GameLoop()
    {
        SetTurnNumber(1);

        while (true)
        {
            // ---- PLAYER TURN ----
            SyncUnitListsWithGrid();
            foreach (var u in playerUnits)
                if (u != null && u.IsAlive) u.ResetForTurn();
            SetState(TurnState.PlayerTurn);

            selector.EnableInput(true);
            SetNextTurnButtonInteractable(true);

            nextTurnRequested = false;
            while (!nextTurnRequested) yield return null;

            SetNextTurnButtonInteractable(false);
            selector.EnableInput(false);

            if (CheckBattleEnd()) yield break;

            // ---- ENEMY TURN ----
            SyncUnitListsWithGrid();
            foreach (var u in enemyUnits)
                if (u != null && u.IsAlive) u.ResetForTurn();
            SetState(TurnState.EnemyTurn);

            yield return StartCoroutine(EnemyTurnRoutine());
            if (CheckBattleEnd()) yield break;

            SetTurnNumber(TurnNumber + 1);
        }
    }

    // ==================================================================
    //  ENEMY AI
    // ==================================================================

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(enemyStartDelay);

        // Snapshot so summons during this phase don't extend the loop
        var snapshot = new List<Unit>(enemyUnits);

        foreach (var enemy in snapshot)
        {
            if (enemy == null || !enemy.IsAlive) continue;

            Unit target = FindClosestPlayer(enemy);
            if (target == null) yield break;

            if (!CanAttack(enemy, target) && enemy.CanMove)
            {
                var costs = grid.GetReachableCosts(enemy);
                HexCoord bestCell = enemy.Cell;
                int bestDist = HexCoord.Distance(enemy.Cell, target.Cell);
                int bestCost = 0;

                foreach (var kv in costs)
                {
                    int d = HexCoord.Distance(kv.Key, target.Cell);
                    if (d < bestDist) { bestDist = d; bestCell = kv.Key; bestCost = kv.Value; }
                }

                if (!bestCell.Equals(enemy.Cell))
                {
                    grid.ClearUnit(enemy.Cell);
                    enemy.SetCell(bestCell);
                    grid.SetUnit(bestCell, enemy);
                    enemy.SnapTo(grid.CellToWorld(bestCell));
                    enemy.SpendMovement(bestCost);
                    yield return new WaitForSeconds(enemyStepDelay);
                }
            }

            if (!enemy.HasAttacked && CanAttack(enemy, target))
            {
                StatsManager.Instance.ResolveAttack(enemy, target);
                enemy.MarkAttacked();
                yield return new WaitForSeconds(enemyStepDelay);
            }
        }
    }

    private Unit FindClosestPlayer(Unit enemy)
    {
        Unit best = null;
        int bestDist = int.MaxValue;
        foreach (var p in playerUnits)
        {
            if (p == null || !p.IsAlive) continue;
            int d = HexCoord.Distance(enemy.Cell, p.Cell);
            if (d < bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    private bool CanAttack(Unit a, Unit b)
    {
        if (a == null || b == null || !a.IsAlive || !b.IsAlive) return false;
        if (a.Faction == b.Faction) return false;
        return HexCoord.Distance(a.Cell, b.Cell) <= a.AttackRange;
    }

    // ==================================================================
    //  BATTLE END
    // ==================================================================

    private bool CheckBattleEnd()
    {
        SyncUnitListsWithGrid();

        bool playersDead = playerUnits.TrueForAll(u => u == null || !u.IsAlive);
        bool enemiesDead = enemyUnits.TrueForAll(u => u == null || !u.IsAlive);

        if (playersDead) { EndGame(TurnState.Defeat); Debug.Log("=== DEFEAT ==="); return true; }
        if (enemiesDead) { EndGame(TurnState.Victory); Debug.Log("=== VICTORY ==="); return true; }
        return false;
    }

    private void EndGame(TurnState endState)
    {
        SetState(endState);
        SetNextTurnButtonInteractable(false);

        if (stopPlayerInputOnGameEnd && selector != null)
            selector.EnableInput(false);

        if (endState == TurnState.Victory) onVictory?.Invoke();
        else if (endState == TurnState.Defeat) onDefeat?.Invoke();
    }

    // ==================================================================
    //  HELPERS
    // ==================================================================

    private int CountStillActionablePlayers()
    {
        int n = 0;
        foreach (var u in playerUnits)
        {
            if (u == null || !u.IsAlive) continue;
            bool stillCan = checkMovementInsteadOfActions ? u.CanMove : u.CanStillAct;
            if (stillCan) n++;
        }
        return n;
    }

    private void SetNextTurnButtonInteractable(bool on)
    {
        if (nextTurnButton != null) nextTurnButton.interactable = on;
    }

    private void SetTurnNumber(int n)
    {
        TurnNumber = n;
        OnTurnChanged?.Invoke(n);
        if (turnLabel != null) turnLabel.text = $"Turn {n}";
    }

    private void SetState(TurnState s)
    {
        CurrentState = s;
        lastCanAct = -1;
        lastAlive = -1;

        OnStateChanged?.Invoke(s);

        if (phaseLabel != null)
            phaseLabel.text = s switch
            {
                TurnState.PlayerTurn => "Player Turn",
                TurnState.EnemyTurn => "Enemy Turn",
                TurnState.Victory => "Victory!",
                TurnState.Defeat => "Defeat…",
                _ => string.Empty
            };

        RefreshUnitsUI();
    }

    private void RefreshUnitsUI()
    {
        if (unitsLabel == null && OnUnitCountChanged == null) return;

        List<Unit> side = CurrentState == TurnState.EnemyTurn ? enemyUnits : playerUnits;

        int alive = 0, canAct = 0;
        foreach (var u in side)
        {
            if (u == null || !u.IsAlive) continue;
            alive++;
            bool stillCan = checkMovementInsteadOfActions ? u.CanMove : u.CanStillAct;
            if (stillCan) canAct++;
        }

        if (canAct == lastCanAct && alive == lastAlive) return;
        lastCanAct = canAct;
        lastAlive = alive;

        if (unitsLabel != null)
        {
            string verb = checkMovementInsteadOfActions ? "can move" : "can act";
            unitsLabel.text = $"{canAct} / {alive} units {verb}";
        }

        OnUnitCountChanged?.Invoke(canAct, alive);
    }
}