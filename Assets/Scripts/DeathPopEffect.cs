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
    private const string MATERIAL_RESOURCE_PATH = "Effects/DeathPopEffect";
    private static bool materialLoadAttempted;

    public static void Create(Vector3 position, Color color)
    {
        DeathPopEffect effect = null;
        GameObject createdEffectObject = null;
        try
        {
            // The Resources material keeps the URP Unlit shader as a build dependency.
            // If the asset or shader is unavailable on a target, omit this cosmetic effect.
            if (!TryLoadSharedMaterial())
            {
                return;
            }

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
                createdEffectObject = new GameObject("DeathPopEffect");
                effect = createdEffectObject.AddComponent<DeathPopEffect>();
            }

            // A pooled instance can predate a static-state reset, and an earlier
            // Initialize can have failed partway through. Rebuild the shard array before
            // Activate/Update are allowed to touch it.
            if (!effect.IsInitialized)
            {
                effect.Initialize();
            }

            if (!effect.IsInitialized)
            {
                DestroyFailedEffect(effect.gameObject);
                return;
            }

            effect.Activate(position, color);
        }
        catch (System.Exception)
        {
            // Death pops are optional. Keep an exception in construction/activation from
            // interrupting the caller, and remove any partially built or pooled object.
            GameObject failedEffectObject = createdEffectObject;
            if (effect != null)
            {
                try
                {
                    failedEffectObject = effect.gameObject;
                }
                catch (System.Exception)
                {
                    // Fall back to the new GameObject reference if the component is invalid.
                }
            }

            DestroyFailedEffect(failedEffectObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        pool.Clear();
        sharedMaterial = null;
        propBlock = null;
        materialLoadAttempted = false;
    }

    private bool IsInitialized => shards != null && shards.Length > 0;

    private static bool TryLoadSharedMaterial()
    {
        if (!materialLoadAttempted)
        {
            materialLoadAttempted = true;
            sharedMaterial = Resources.Load<Material>(MATERIAL_RESOURCE_PATH);
        }

        return sharedMaterial != null &&
               sharedMaterial.shader != null &&
               sharedMaterial.shader.isSupported;
    }

    private static void DestroyFailedEffect(GameObject failedEffectObject)
    {
        if (failedEffectObject == null)
        {
            return;
        }

        try
        {
            failedEffectObject.SetActive(false);
        }
        catch (System.Exception)
        {
            // Continue to destruction even if lifecycle callbacks reject deactivation.
        }

        try
        {
            Destroy(failedEffectObject);
        }
        catch (System.Exception)
        {
            // Cleanup is best-effort too; never propagate a cosmetic failure to gameplay.
        }
    }

    private void Initialize()
    {
        if (sharedMaterial == null || sharedMaterial.shader == null || !sharedMaterial.shader.isSupported)
        {
            return;
        }

        // Static state is created before anything that can fail, so a later failure
        // can never leave Create/Activate dereferencing a null property block.
        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        int count = 6;
        shards = new Shard[count];

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

        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        // Set color via MaterialPropertyBlock to avoid cloning Material instances
        propBlock.SetColor("_Color", color);
        propBlock.SetColor("_BaseColor", color);

        for (int i = 0; i < shards.Length; i++)
        {
            if (shards[i].transform == null)
            {
                continue;
            }

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
        // An effect whose Initialize never completed has no shards to animate; park
        // it instead of dereferencing a null shard transform every frame.
        if (!IsInitialized)
        {
            gameObject.SetActive(false);
            return;
        }

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
            Transform shardTransform = shards[i].transform;
            if (shardTransform == null)
            {
                continue;
            }

            shards[i].velocity += gravity * Time.deltaTime;
            shardTransform.localPosition += shards[i].velocity * Time.deltaTime;

            // Shrink the shards as they fall/explode
            shardTransform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0f, progress);
        }
    }
}
