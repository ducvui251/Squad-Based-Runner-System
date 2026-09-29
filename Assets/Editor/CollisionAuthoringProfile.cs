using System;
using UnityEditor;
using UnityEngine;

// Opt-in editor data. Nothing runs on import or during gameplay.
[CreateAssetMenu(menuName = "SpiralSquad/Collision Authoring Profile")]
public sealed class CollisionAuthoringProfile : ScriptableObject
{
    public enum Role { Unspecified, Support, Blocker, Trigger, ScriptedDamage, Decoration }
    public enum Shape { Box, Sphere, Capsule }

    [Serializable]
    public sealed class Entry
    {
        public GameObject prefab;
        public string targetPath;
        public Role role;
        public Shape shape;
        public bool manualOverride;
        [TextArea] public string exceptionReason;
        public bool fitMeshes;
        [Tooltip("Explicit relative paths below the target; exclude VFX, labels and support art.")]
        public string[] meshPaths = Array.Empty<string>();
        public Vector3 center;
        public Vector3 size = Vector3.one;
        public float radius = 0.5f;
        public float height = 2f;
        public int layer;
        [Tooltip("Scripted damage uses explicit values, in the consumer's existing coordinate units.")]
        public string consumerType;
        public string centerProperty;
        public string sizeProperty;
        public string radiusProperty;
        [HideInInspector] public int generationVersion;
    }

    public Entry[] entries = Array.Empty<Entry>();
}
