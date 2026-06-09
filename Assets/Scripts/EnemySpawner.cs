using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject enemyPrefab;      // Prefab của Enemy cần sinh
    [SerializeField] private int spawnAmount = 5;         // Số lượng enemy muốn sinh ra
    [SerializeField] private float spacing = 0.6f;        // Khoảng cách giãn cách giữa các enemy
    [SerializeField] private Transform parentFolder;      // Thư mục cha để chứa các enemy sinh ra (tùy chọn)

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab chưa được gán trong Inspector!");
            return;
        }

        if (parentFolder == null)
        {
            parentFolder = transform;
        }

        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        // Sắp xếp các enemy theo dạng hình xoắn ốc Fermat để tạo thành cụm đều đẹp
        float goldenAngle = 137.5f;

        for (int i = 0; i < spawnAmount; i++)
        {
            // Tính toán tọa độ xoắn ốc Fermat
            float distance = spacing * Mathf.Sqrt(i + 1);
            float angle = (i + 1) * goldenAngle * Mathf.Deg2Rad;

            float x = distance * Mathf.Cos(angle);
            float z = distance * Mathf.Sin(angle);

            Vector3 localPos = new Vector3(x, 0f, z);
            Vector3 worldPos = transform.TransformPoint(localPos);

            // Sinh enemy ra màn chơi
            GameObject enemy = Instantiate(enemyPrefab, worldPos, Quaternion.identity, parentFolder);
            
            // Xoay hướng mặt của enemy đồng bộ với Spawner
            enemy.transform.forward = transform.forward;
        }
    }

    private void OnDrawGizmos()
    {
        // Vẽ khối vuông biểu thị vị trí Spawner trong Scene Editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.8f, 0.8f, 0.8f));

        // Vẽ nháp các điểm spawn dự kiến
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        float goldenAngle = 137.5f;
        for (int i = 0; i < Mathf.Min(spawnAmount, 20); i++)
        {
            float distance = spacing * Mathf.Sqrt(i + 1);
            float angle = (i + 1) * goldenAngle * Mathf.Deg2Rad;
            float x = distance * Mathf.Cos(angle);
            float z = distance * Mathf.Sin(angle);
            Gizmos.DrawSphere(transform.TransformPoint(new Vector3(x, 0f, z)), 0.2f);
        }
    }
}
