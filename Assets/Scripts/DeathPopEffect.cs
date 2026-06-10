using System.Collections.Generic;
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
    
    // Static reusable pool & material caching to completely eliminate runtime GC allocations and file checks
    private static Queue<DeathPopEffect> pool = new Queue<DeathPopEffect>();
    private static Material sharedMaterial;
    private static MaterialPropertyBlock propBlock;

    public static void Create(Vector3 position, Color color)
    {
        DeathPopEffect effect = null;
        while (pool.Count > 0)
        {
            effect = pool.Dequeue();
            if (effect != null)
            {
                effect.gameObject.SetActive(true);
                effect.transform.position = position;
                break;
            }
        }

        if (effect == null)
        {
            GameObject g = new GameObject("DeathPopEffect");
            effect = g.AddComponent<DeathPopEffect>();
            effect.Initialize();
        }

        effect.Activate(position, color);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        pool.Clear();
        sharedMaterial = null;
        propBlock = null;
    }

    private void Initialize()
    {
        int count = 6;
        shards = new Shard[count];

        if (sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            sharedMaterial = new Material(shader);
        }

        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

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
                r.sharedMaterial = sharedMaterial;
            }

            shards[i] = new Shard
            {
                transform = shardObj.transform,
                velocity = Vector3.zero
            };
        }
    }

    private void Activate(Vector3 position, Color color)
    {
        timer = 0f;
        transform.position = position;

        // Set color via MaterialPropertyBlock to avoid cloning Material instances
        propBlock.SetColor("_Color", color);
        propBlock.SetColor("_BaseColor", color);

        for (int i = 0; i < shards.Length; i++)
        {
            shards[i].transform.localPosition = Vector3.zero;
            shards[i].transform.localScale = Vector3.one * Random.Range(0.15f, 0.3f);
            shards[i].velocity = new Vector3(
                Random.Range(-3f, 3f),
                Random.Range(3f, 8f),
                Random.Range(-3f, 3f)
            );

            Renderer r = shards[i].transform.GetComponent<Renderer>();
            if (r != null)
            {
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / DURATION;

        if (progress >= 1f)
        {
            gameObject.SetActive(false);
            pool.Enqueue(this);
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
