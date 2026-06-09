using UnityEngine;

public class DeathPopEffect : MonoBehaviour
{
    private struct Shard
    {
        public Transform transform;
        public Vector3 velocity;
    }

    private Shard[] shards;
    private float timer = 0f;
    private const float DURATION = 0.5f;
    private Material tempMaterial;

    public static void Create(Vector3 position, Color color)
    {
        GameObject g = new GameObject("DeathPopEffect");
        g.transform.position = position;
        DeathPopEffect effect = g.AddComponent<DeathPopEffect>();
        effect.Initialize(color);
    }

    private void Initialize(Color color)
    {
        int count = 6;
        shards = new Shard[count];

        // Find URP/Unlit shader, fallback to standard Unlit/Color if not found
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        tempMaterial = new Material(shader);
        tempMaterial.color = color;

        for (int i = 0; i < count; i++)
        {
            GameObject shardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Remove collider immediately to prevent any physics overhead or triggers
            Collider col = shardObj.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            shardObj.transform.SetParent(transform);
            shardObj.transform.localPosition = Vector3.zero;
            shardObj.transform.localScale = Vector3.one * Random.Range(0.15f, 0.3f);
            
            Renderer r = shardObj.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = tempMaterial;
            }

            shards[i] = new Shard
            {
                transform = shardObj.transform,
                velocity = new Vector3(
                    Random.Range(-3f, 3f),
                    Random.Range(3f, 8f),
                    Random.Range(-3f, 3f)
                )
            };
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / DURATION;

        if (progress >= 1f)
        {
            if (tempMaterial != null)
            {
                Destroy(tempMaterial); // Prevent material leaks
            }
            Destroy(gameObject);
            return;
        }

        Vector3 gravity = new Vector3(0f, -18f, 0f);
        for (int i = 0; i < shards.Length; i++)
        {
            shards[i].velocity += gravity * Time.deltaTime;
            shards[i].transform.localPosition += shards[i].velocity * Time.deltaTime;
            
            // Shrink the shards as they fall/explode
            shards[i].transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0f, progress);
        }
    }
}
