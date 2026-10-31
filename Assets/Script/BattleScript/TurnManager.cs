using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum TurnState { Preparing, PlayerTurn, EnemyTurn, Victory, Defeat }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private HexGridManager grid;
    [SerializeField] private HexSelector selector;

    [Header("Units (manually placed — legacy)")]
    [SerializeField] private List<Unit> playerUnits = new();
    [SerializeField] private List<Unit> enemyUnits = new();

    [Header("Preparing Stage")]
    [SerializeField] private bool requiresPreparingStage = true;

    [Header("AI Timing")]
    [SerializeField] private float enemyStartDelay = 0.30f;
    [SerializeField] private float enemyStepDelay = 0.25f;

    [Header("Battle End Cleanup")]
    [SerializeField] private float transientCleanupDelay = 0.4f;

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

    public event Action<int> OnTurnChanged;
    public event Action<TurnState> OnStateChanged;
    public event Action<int, int> OnUnitCountChanged;

    public int TurnNumber { get; private set; }
    public TurnState CurrentState { get; private set; }
    public bool IsPlayerTurn => CurrentState == TurnState.PlayerTurn;

    private bool nextTurnRequested;
    private bool popupOpen;
    private bool gameLoopRunning;
    private int lastCanAct = -1;
    private int lastAlive = -1;

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

        foreach (var u in playerUnits)
            if (u != null && grid != null) grid.RegisterUnit(u, grid.WorldToCell(u.transform.position));
        foreach (var u in enemyUnits)
            if (u != null && grid != null) grid.RegisterUnit(u, grid.WorldToCell(u.transform.position));

        if (grid != null) grid.OnUnitDied += HandleUnitDied;

        SyncUnitListsWithGrid();
        SetNextTurnButtonInteractable(false);

        if (requiresPreparingStage)
        {
            var prep = FindAnyObjectByType<PreparingController>();
            if (prep == null) BeginNewBattle();
            else SetState(TurnState.Preparing);
        }
        else BeginNewBattle();
    }

    private void OnDestroy()
    {
        if (grid != null) grid.OnUnitDied -= HandleUnitDied;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (CurrentState == TurnState.PlayerTurn && !popupOpen)
        {
            SyncUnitListsWithGrid();
            RefreshUnitsUI();
        }
    }

    // ★ NEW: Resets everything before starting a new battle
    public void ResetForNewBattle()
    {
        StopAllCoroutines(); // Kill any lingering loops from the previous fight
        gameLoopRunning = false;
        nextTurnRequested = false;
        popupOpen = false;

        playerUnits.Clear();
        enemyUnits.Clear();

        TurnNumber = 1;
        SetState(TurnState.Preparing);
        SetNextTurnButtonInteractable(false);
        if (selector != null) selector.EnableInput(false);
    }

    public void BeginNewBattle()
    {
        if (gameLoopRunning) return;

        // ★ Force the lists to match the grid before starting
        SyncUnitListsWithGrid();

        StartCoroutine(GameLoop());
    }

    public void RegisterSpawnedUnit(Unit unit)
    {
        if (unit == null) return;
        if (unit.Faction == Faction.Player) { if (!playerUnits.Contains(unit)) playerUnits.Add(unit); }
        else { if (!enemyUnits.Contains(unit)) enemyUnits.Add(unit); }
        RefreshUnitsUI();
    }

    public void SyncUnitListsWithGrid()
    {
        if (grid == null) return;
        playerUnits.RemoveAll(u => u == null);
        enemyUnits.RemoveAll(u => u == null);

        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);
            if (u == null || !u.IsAlive) continue;
            if (u.Faction == Faction.Player) { if (!playerUnits.Contains(u)) playerUnits.Add(u); }
            else { if (!enemyUnits.Contains(u)) enemyUnits.Add(u); }
        }
    }

    private void HandleUnitDied(Unit unit)
    {
        if (unit == null) return;
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);
        RefreshUnitsUI();

        if (CurrentState == TurnState.PlayerTurn || CurrentState == TurnState.EnemyTurn)
        {
            CheckBattleEnd();
        }
    }

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
        else ConfirmEndTurn();
    }

    public void ConfirmEndTurn() { popupOpen = false; nextTurnRequested = true; }
    public void CancelEndTurn() { popupOpen = false; }

    private IEnumerator GameLoop()
    {
        gameLoopRunning = true;
        SetTurnNumber(1);

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.ResetBattleTrackers();

        while (true)
        {
            SyncUnitListsWithGrid();
            foreach (var u in playerUnits) if (u?.IsAlive == true) u.ResetForTurn();
            ApplyStartOfTurnPassives(playerUnits);
            SummonManager.ApplyStartOfTurnSummons(playerUnits, TurnNumber, grid);
            SetState(TurnState.PlayerTurn);

            selector?.EnableInput(true);
            SetNextTurnButtonInteractable(true);

            nextTurnRequested = false;
            while (!nextTurnRequested) yield return null;

            SetNextTurnButtonInteractable(false);
            selector?.EnableInput(false);

            if (!gameLoopRunning) yield break;
            if (CheckBattleEnd()) { gameLoopRunning = false; yield break; }

            SyncUnitListsWithGrid();
            foreach (var u in enemyUnits) if (u?.IsAlive == true) u.ResetForTurn();
            ApplyStartOfTurnPassives(enemyUnits);
            SummonManager.ApplyStartOfTurnSummons(enemyUnits, TurnNumber, grid);
            SetState(TurnState.EnemyTurn);

            yield return StartCoroutine(EnemyTurnRoutine());

            if (!gameLoopRunning) yield break;
            if (CheckBattleEnd()) { gameLoopRunning = false; yield break; }

            SetTurnNumber(TurnNumber + 1);
        }
    }

    private void ApplyStartOfTurnPassives(List<Unit> units)
    {
        if (units == null) return;
        foreach (var u in units)
        {
            if (u == null || !u.IsAlive || u.Definition == null) continue;
            if (u.Definition.ability == SpecialAbility.SelfHeal)
            {
                int heal = Mathf.Max(1, u.Attack / 2);
                u.Heal(heal);
            }
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(enemyStartDelay);
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
                    if (grid.TerrainMap != null)
                    {
                        int dmg = grid.TerrainMap.GetEntryDamage(bestCell);
                        if (dmg > 0) enemy.TakeDamage(dmg);
                    }
                    yield return new WaitForSeconds(enemyStepDelay);
                    if (!enemy.IsAlive) continue;
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
        Unit best = null; int bestDist = int.MaxValue;
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

    private bool CheckBattleEnd()
    {
        SyncUnitListsWithGrid();
        bool anyPlayerAlive = false;
        foreach (var u in playerUnits)
        {
            if (u == null || !u.IsAlive || u.IsTransient) continue;
            anyPlayerAlive = true; break;
        }
        bool anyEnemyAlive = false;
        foreach (var u in enemyUnits)
        {
            if (u == null || !u.IsAlive || u.IsTransient) continue;
            anyEnemyAlive = true; break;
        }

        if (!anyPlayerAlive) { EndGame(TurnState.Defeat); return true; }
        if (!anyEnemyAlive) { EndGame(TurnState.Victory); return true; }
        return false;
    }

    private void EndGame(TurnState endState)
    {
        SetNextTurnButtonInteractable(false);
        if (stopPlayerInputOnGameEnd && selector != null) selector.EnableInput(false);
        StartCoroutine(EndGameRoutine(endState));
    }

    private IEnumerator EndGameRoutine(TurnState endState)
    {
        yield return StartCoroutine(CleanupTransientUnitsRoutine());
        SetState(endState);

        if (endState == TurnState.Victory)
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.AwardBattleEndGold(playerUnits);
            onVictory?.Invoke();
        }
        else if (endState == TurnState.Defeat)
        {
            onDefeat?.Invoke();
        }
    }

    private IEnumerator CleanupTransientUnitsRoutine()
    {
        if (transientCleanupDelay > 0f)
            yield return new WaitForSecondsRealtime(transientCleanupDelay);

        var toDestroy = new List<Unit>();
        foreach (var u in playerUnits) if (u != null && u.IsTransient) toDestroy.Add(u);
        foreach (var u in enemyUnits) if (u != null && u.IsTransient) toDestroy.Add(u);
        if (toDestroy.Count == 0) yield break;

        foreach (var u in toDestroy)
        {
            if (u == null) continue;
            if (grid != null) grid.ClearUnit(u.Cell);
            playerUnits.Remove(u);
            enemyUnits.Remove(u);
            if (u.gameObject != null) Destroy(u.gameObject);
        }
        RefreshUnitsUI();
    }

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
                TurnState.Preparing => "Preparing…",
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
        if (CurrentState == TurnState.Preparing)
        {
            if (unitsLabel != null) unitsLabel.text = string.Empty;
            return;
        }

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