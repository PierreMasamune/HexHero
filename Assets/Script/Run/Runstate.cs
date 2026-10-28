using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunState
{
    [Serializable] public class ItemStack { public string itemId; public int count; }
    [Serializable] public class NodeProgress { public string nodeId; public bool visited; }

    [Header("Meta")]
    public int seed;
    public int chapter = 1;
    public int turnNumber = 1;

    [Header("Money")]
    public int money;

    [Header("Units")]
    public List<UnitInstance> units = new();
    public int unitCapBonus = 0;

    [Header("Items")]
    public List<ItemStack> items = new();

    [Header("Buffs")]
    public List<string> activeBuffIds = new();

    [Header("Map Progress")]
    public string currentMapNodeId = "";
    public List<string> visitedMapNodeIds = new();
    public int runDepth = 0;

    [Header("Flags")]
    public bool isRunOver = false;
    public bool isVictory = false;

    public RunState Clone()
    {
        string json = JsonUtility.ToJson(this);
        return JsonUtility.FromJson<RunState>(json);
    }
}