using UnityEngine;
using TMPro;

public class Door : MonoBehaviour
{
    public enum GateType { Add, Multiply }

    [System.Serializable]
    public struct GateSettings
    {
        public GateType type;
        public int value;
    }

    [Header(" Bonuses ")]
    [SerializeField] private bool randomBonuses; // Kept to match prefab layout
    [SerializeField] private GateSettings leftGate;
    [SerializeField] private GateSettings rightGate;

    [Header(" Components ")]
    [SerializeField] private Collider[] doorsColliders;
    [SerializeField] private TextMeshPro rightDoorText;
    [SerializeField] private TextMeshPro leftDoorText;

    private bool isTriggered = false;

    private void Start()
    {
        ConfigureBonusTexts();
    }

    private void ConfigureBonusTexts()
    {
        if (rightDoorText != null)
        {
            rightDoorText.text = GetGateString(rightGate);
        }
        if (leftDoorText != null)
        {
            leftDoorText.text = GetGateString(leftGate);
        }
    }

    private string GetGateString(GateSettings gate)
    {
        return (gate.type == GateType.Add) ? "+" + gate.value : "x" + gate.value;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Physics Trigger: Gate touched by " + other.name);

        if (isTriggered) return;

        // Find the crowd manager in the trigger object
        PlayerCrowdManager crowdManager = other.GetComponentInParent<PlayerCrowdManager>();
        if (crowdManager != null)
        {
            Debug.Log("Crowd Manager Found! Applying math: Left (" + leftGate.type + " " + leftGate.value + "), Right (" + rightGate.type + " " + rightGate.value + ")");
            isTriggered = true; // Prevent double-triggering
            DisableDoors();     // Turn off both doors

            // Determine if the player ran through the left or right door based on relative X position
            float relativeX = other.transform.position.x - transform.position.x;
            
            GateSettings selectedGate = (relativeX > 0) ? rightGate : leftGate;

            // Apply crowd math directly
            if (selectedGate.type == GateType.Add)
            {
                crowdManager.SpawnClones(selectedGate.value);
            }
            else if (selectedGate.type == GateType.Multiply)
            {
                crowdManager.MultiplyClones(selectedGate.value);
            }
        }
        else
        {
            Debug.LogWarning("Trigger failed: PlayerCrowdManager component was not found on " + other.name + " or its parents!");
        }
    }

    private void DisableDoors()
    {
        foreach (Collider c in doorsColliders)
        {
            if (c != null)
            {
                c.enabled = false;
            }
        }
    }
}
