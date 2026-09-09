using UnityEngine;

public class ConeHazard : MonoBehaviour
{
    [Header("Kill Settings")]
    [SerializeField] private float killRadius = 0.85f; // Contact radius: any clone this close dies

    [Header("Boundary Elimination")]
    [SerializeField] private float boundaryX = 5f;
    [SerializeField] private float boundaryEliminationMargin = 0.25f;

    private PlayerCrowdManager cachedCrowdManager;

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        var runners = cachedCrowdManager.ActiveRunners;
        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;

            GameObject runner = runners[i];
            Vector3 runnerPosition = runner.transform.position;
            Vector3 flatDelta = runnerPosition - transform.position;
            flatDelta.y = 0f;

            // Kill on contact: any clone touching the cone is destroyed.
            bool touching = flatDelta.sqrMagnitude <= killRadius * killRadius;

            // Safety net: eliminate clones that end up off the road edge near this
            // cone. Without the Z gate, every cone in the scene can kill a runner
            // anywhere on the level as soon as its X position crosses the boundary.
            bool nearCone = Mathf.Abs(runnerPosition.z - transform.position.z) <=
                killRadius + boundaryEliminationMargin;
            bool beyondRoadEdge = nearCone &&
                Mathf.Abs(runnerPosition.x) >= boundaryX - boundaryEliminationMargin;

            if (touching || beyondRoadEdge)
            {
                Debug.Log("CONE_KILL: " + name + " pos=" + transform.position
                    + " killed '" + runner.name + "' at " + runnerPosition
                    + " touching=" + touching + " |x|=" + Mathf.Abs(runnerPosition.x).ToString("F2")
                    + " killRadius=" + killRadius, this);
                cachedCrowdManager.RemoveRunnerByHazard(
                    runner,
                    runnerPosition + Vector3.up * 0.5f,
                    GetRunnerColor(runner, Color.blue));
            }
        }
    }

    private static Color GetRunnerColor(GameObject runner, Color fallback)
    {
        Renderer renderer = runner != null ? runner.GetComponentInChildren<Renderer>() : null;
        if (renderer != null && renderer.sharedMaterial != null)
        {
            if (renderer.sharedMaterial.HasProperty("_Color"))
            {
                return renderer.sharedMaterial.color;
            }

            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                return renderer.sharedMaterial.GetColor("_BaseColor");
            }
        }

        return fallback;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}
