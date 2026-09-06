using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic, allocation-light object pool for any <see cref="Component"/>.
/// Reuses instances to eliminate runtime Instantiate/Destroy churn and the GC
/// pressure that comes with mass-scale spawning (clones, projectiles, effects).
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> available = new Queue<T>();
    private readonly Func<T> createFunc;
    private readonly Action<T> onGet;
    private readonly Action<T> onRelease;
    private readonly int maxSize;

    /// <summary>Number of pooled instances currently idle and ready to reuse.</summary>
    public int CountInactive => available.Count;

    /// <param name="createFunc">Factory used when the queue is empty. Required.</param>
    /// <param name="onGet">Called right after an instance is pulled out (e.g. SetActive(true)).</param>
    /// <param name="onRelease">Called when an instance is returned (e.g. SetActive(false)).</param>
    /// <param name="maxSize">Hard cap on pooled instances; releases beyond it are destroyed.</param>
    public ObjectPool(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, int maxSize = 64)
    {
        if (createFunc == null) throw new ArgumentNullException(nameof(createFunc));
        this.createFunc = createFunc;
        this.onGet = onGet;
        this.onRelease = onRelease;
        this.maxSize = Mathf.Max(1, maxSize);
    }

    /// <summary>Pre-create instances so the first Get() doesn't pay a one-frame creation spike.</summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T item = createFunc();
            onRelease?.Invoke(item);
            available.Enqueue(item);
        }
    }

    /// <summary>
    /// Rent an instance, reusing a pooled one when available. Destroyed pooled
    /// instances (e.g. after a scene or play-mode teardown) are skipped so a
    /// stale reference is never handed back.
    /// </summary>
    public T Get()
    {
        T item = null;
        while (available.Count > 0)
        {
            item = available.Dequeue();
            if (item != null) break; // Unity's == null also matches destroyed objects.
            item = null;             // Destroyed pooled instance — drop and keep looking.
        }

        if (item == null)
        {
            item = createFunc();
        }

        onGet?.Invoke(item);
        return item;
    }

    /// <summary>Return an instance to the pool, or destroy it if the cap is reached.</summary>
    public void Release(T item)
    {
        if (item == null) return;

        onRelease?.Invoke(item);

        if (available.Count < maxSize)
        {
            available.Enqueue(item);
        }
        else
        {
            UnityEngine.Object.Destroy(item.gameObject);
        }
    }

    /// <summary>Drop all idle instances. Used to reset static pools on play-mode reload.</summary>
    public void Clear()
    {
        available.Clear();
    }
}
