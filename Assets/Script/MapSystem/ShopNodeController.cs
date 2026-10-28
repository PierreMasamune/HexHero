using System;
using UnityEngine;

public class ShopNodeController : MonoBehaviour, IMapNodeController
{
    [Header("References")]
    [SerializeField] private ShopUI shopUI;

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;

        if (shopUI != null)
        {
            shopUI.OnShopClosed += HandleShopClosed;
            shopUI.OpenShop(); // ★ Open the shop window
        }
        else
        {
            Debug.LogWarning("[ShopNodeController] No ShopUI assigned. Skipping node.");
            OnComplete?.Invoke(this, MapNodeResult.Success);
        }
    }

    private void HandleShopClosed()
    {
        if (shopUI != null) shopUI.OnShopClosed -= HandleShopClosed;

        // Player finished shopping, return to map
        OnComplete?.Invoke(this, MapNodeResult.Success);
    }

    public void RaiseComplete(MapNodeResult result)
    {
        if (shopUI != null) shopUI.OnShopClosed -= HandleShopClosed;
        OnComplete?.Invoke(this, result);
    }
}