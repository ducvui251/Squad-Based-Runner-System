using UnityEngine;

[CreateAssetMenu(fileName = "SkinAvatarCatalog", menuName = "SpiralSquad/Skin Avatar Catalog")]
public sealed class SkinAvatarCatalog : ScriptableObject
{
    public const int SkinCount = 9;

    [SerializeField] private GameObject[] skinModels = new GameObject[SkinCount];

    public GameObject GetModel(int skinId)
    {
        return skinModels != null && skinId >= 0 && skinId < skinModels.Length
            ? skinModels[skinId]
            : null;
    }
}
