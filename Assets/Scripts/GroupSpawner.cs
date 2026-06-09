using UnityEngine;

public class GroupSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject objectToSpawn; // Prefab of the Enemy
    [SerializeField] private int amount = 10;            // Number of enemies to spawn
    [SerializeField] private Transform parentFolder;     // Container to keep hierarchy clean

    [Header("Fermat Spiral Settings")]
    [SerializeField] private float radiusFactor = 0.5f;  // Spacing distance between spawned enemies
    [SerializeField] private float angleFactor = 1f;

    private void Start()
    {
        if (objectToSpawn == null)
        {
            Debug.LogError("GroupSpawner: objectToSpawn is not assigned!");
            return;
        }

        // Default parent to this transform if not assigned
        if (parentFolder == null)
        {
            parentFolder = transform;
        }

        SpawnGroup();
    }

    private void Update()
    {
        // Once all spawned enemies are dead, destroy the spawner itself
        if (parentFolder != null && parentFolder.childCount == 0)
        {
            Destroy(gameObject);
        }
    }

    private void SpawnGroup()
    {
        float goldenAngle = 137.5f * angleFactor;

        for (int i = 0; i < amount; i++)
        {
            // Calculate Fermat spiral coordinates
            float distance = radiusFactor * Mathf.Sqrt(i + 1);
            float angle = (i + 1) * goldenAngle * Mathf.Deg2Rad;

            float x = distance * Mathf.Cos(angle);
            float z = distance * Mathf.Sin(angle);

            Vector3 localPos = new Vector3(x, 0f, z);
            Vector3 worldPos = transform.TransformPoint(localPos);

            // Spawn the enemy prefab
            GameObject enemy = Instantiate(objectToSpawn, worldPos, Quaternion.identity, parentFolder);
            
            // Adjust enemy facing direction to look slightly forward relative to the spawner
            enemy.transform.forward = transform.forward;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw a visual marker in the Scene View to show where the spawner is placed
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1f, 1f, 1f));

        // Draw circles showing spawn area in Scene View
        float goldenAngle = 137.5f * angleFactor;
        for (int i = 0; i < Mathf.Min(amount, 10); i++)
        {
            float distance = radiusFactor * Mathf.Sqrt(i + 1);
            float angle = (i + 1) * goldenAngle * Mathf.Deg2Rad;
            float x = distance * Mathf.Cos(angle);
            float z = distance * Mathf.Sin(angle);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.TransformPoint(new Vector3(x, 0f, z)), 0.2f);
        }
    }
}
