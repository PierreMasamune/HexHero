using UnityEngine;

public static class UnitFactory
{
    /// <summary>
    /// Spawn a unit from a UnitDefinition.
    /// Rotation defaults to the prefab's own authored rotation.
    /// </summary>
    public static Unit Spawn(UnitDefinition def, Faction side, Vector3 worldPosition,
                             Quaternion? rotationOverride = null, Transform parent = null)
    {
        if (def == null) { Debug.LogError("[UnitFactory] Spawn called with null definition."); return null; }

        // ---- Pick the prefab ----
        GameObject prefab = def.unitPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/UnitPrefab");
            if (prefab == null)
            {
                Debug.LogError($"[UnitFactory] No prefab for '{def.unitName}'. " +
                               "Set 'unitPrefab' on the asset, or create " +
                               "Assets/Resources/Prefabs/UnitPrefab.prefab as a fallback.");
                return null;
            }
        }

        // ---- Pick the rotation ----
        //    Priority: explicit override > prefab's own rotation
        Quaternion finalRotation = rotationOverride ?? prefab.transform.rotation;

        // ---- Spawn ----
        var go = Object.Instantiate(prefab, worldPosition, finalRotation, parent);
        var unit = go.GetComponent<Unit>();
        if (unit == null) unit = go.AddComponent<Unit>();

        unit.InitializeFrom(def, side);
        go.name = def.unitName;
        return unit;
    }

    /// <summary>
    /// Spawn from a persistent inventory entry (preserves current HP).
    /// </summary>
    public static Unit Spawn(UnitInstance instance, Faction side, Vector3 worldPosition,
                             Quaternion? rotationOverride = null, Transform parent = null)
    {
        if (instance == null || instance.definition == null)
        {
            Debug.LogError("[UnitFactory] Spawn called with null instance or definition.");
            return null;
        }

        var unit = Spawn(instance.definition, side, worldPosition, rotationOverride, parent);
        if (unit != null) unit.InitializeFrom(instance, side);
        return unit;
    }
}