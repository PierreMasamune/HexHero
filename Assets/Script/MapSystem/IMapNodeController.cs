using System;

public enum MapNodeResult
{
    Success,
    Fail,
    Abort
}

public interface IMapNodeController
{
    string CurrentNodeId { get; }
    void Enter(MapNodeData node, RunState state);
    event Action<IMapNodeController, MapNodeResult> OnComplete;
    void RaiseComplete(MapNodeResult result);
}