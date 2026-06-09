using UnityEngine;

public class Gate : MonoBehaviour
{
    public enum GateType { Add, Multiply }

    [Header("Gate Settings")]
    [SerializeField] private GateType gateType = GateType.Add; // Add (+3) or Multiply (x5)
    [SerializeField] private int value = 3;                   // The math value of this gate
    [SerializeField] private Gate siblingGate;                // The other gate next to this one (to disable both on selection)

    private bool isTriggered = false;
    private Collider gateCollider;

    private void Awake()
    {
        gateCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        UpdateGateText();
    }

    /// <summary>
    /// Automatically finds any TextMeshPro component in children and formats the math text.
    /// </summary>
    public void UpdateGateText()
    {
        // Dynamically find any TMPro component on the gate
        TMPro.TMP_Text textComponent = GetComponentInChildren<TMPro.TMP_Text>();
        if (textComponent != null)
        {
            if (gateType == GateType.Add)
            {
                textComponent.text = "+" + value;
            }
            else if (gateType == GateType.Multiply)
            {
                textComponent.text = "x" + value; // e.g. x5
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        // Check if the triggering object has the Crowd Manager component (or is a runner belonging to it)
        PlayerCrowdManager crowdManager = other.GetComponentInParent<PlayerCrowdManager>();
        if (crowdManager != null)
        {
            isTriggered = true; // Prevent double-triggering

            // Apply crowd math
            if (gateType == GateType.Add)
            {
                crowdManager.SpawnClones(value);
            }
            else if (gateType == GateType.Multiply)
            {
                crowdManager.MultiplyClones(value);
            }

            // Deactivate this gate
            DeactivateGate();

            // Deactivate sibling gate next to it so the player can't double-dip!
            if (siblingGate != null)
            {
                siblingGate.DeactivateGate();
            }
        }
    }

    /// <summary>
    /// Disables the collider and hides the gate visuals cleanly.
    /// </summary>
    public void DeactivateGate()
    {
        isTriggered = true;

        if (gateCollider != null)
        {
            gateCollider.enabled = false; // Disable collision so no further triggers occur
        }

        // Hide gate visuals (disable MeshRenderers or children gameobjects)
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
}
