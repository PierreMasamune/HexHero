using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifetime = 1.5f;

    private Color startColor;
    private float timer;

    public static void Spawn(Vector3 position, int damage, bool isCrit, int targetMaxHP)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/DamageText");
        if (prefab == null)
        {
            Debug.LogError("Missing DamageText prefab in Resources/Prefabs/");
            return;
        }

        GameObject go = Instantiate(prefab, position + Vector3.up * 1.5f, Quaternion.identity);
        FloatingDamageText fdt = go.GetComponent<FloatingDamageText>();
        if (fdt != null) fdt.Setup(damage, isCrit, targetMaxHP);
    }

    private void Setup(int damage, bool isCrit, int targetMaxHP)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

        textMesh.text = damage.ToString();
        float damageRatio = (float)damage / targetMaxHP;

        if (isCrit || damageRatio >= 0.4f)
        {
            textMesh.color = Color.red;
            textMesh.fontSize = 10;
            textMesh.text += "!";
        }
        else if (damageRatio >= 0.2f)
        {
            textMesh.color = new Color(1f, 0.5f, 0f); // Orange
            textMesh.fontSize = 7;
        }
        else
        {
            textMesh.color = Color.white;
            textMesh.fontSize = 5;
        }

        startColor = textMesh.color;
        timer = lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            float alpha = Mathf.Clamp01(timer / lifetime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
    }
}