using System;
using UnityEngine;

public class ShopNodeController : MonoBehaviour, IMapNodeController
{
    [Header("Shop UI (assigned to friend's shop panel)")]
    [SerializeField] private GameObject shopRoot;

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;

        if (shopRoot != null) shopRoot.SetActive(true);

        // TODO: wire friend's shop system to call LeaveShop() when the player is done.
        Debug.Log($"[ShopNodeController] Entered shop node {node.id}. " +
                  $"Call LeaveShop() to return to the map.");
    }

    /// <summary>Wire to the shop's "Leave" button or call from friend's shop code when done.</summary>
    public void LeaveShop()
    {
        if (shopRoot != null) shopRoot.SetActive(false);
        OnComplete?.Invoke(this, MapNodeResult.Success);
    }

    public void RaiseComplete(MapNodeResult result)
    {
        if (shopRoot != null) shopRoot.SetActive(false);
        OnComplete?.Invoke(this, result);
    }
}