using System;
using UnityEngine;

public class BuffNodeController : MonoBehaviour, IMapNodeController
{
    [SerializeField] private GameObject buffRoot;
    [SerializeField] private GameObject buffPickCanvas;    // friend's buff picker

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;
        if (buffRoot != null) buffRoot.SetActive(true);
        Debug.Log($"[BuffNodeController] Entered buff node {node.id}.");
    }

    public void LeaveBuffNode()
    {
        if (buffRoot != null) buffRoot.SetActive(false);
        OnComplete?.Invoke(this, MapNodeResult.Success);
    }

    public void RaiseComplete(MapNodeResult result)
    {
        if (buffRoot != null) buffRoot.SetActive(false);
        OnComplete?.Invoke(this, result);
    }
}