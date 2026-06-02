using UnityEngine;

[DisallowMultipleComponent]
public class GravityBody : MonoBehaviour
{
    [Header("Gravity Body")]
    public float gravityStrength = 9.81f;
    public float effectiveRadius = 20f;
    public bool useRadius = false;
    public Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.2f);

    public Vector3 GetGravityCenter()
    {
        return transform.position;
    }

    public Vector3 GetGravityPoint(Vector3 position)
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 closestPoint = col.ClosestPoint(position);
            if ((closestPoint - position).sqrMagnitude > Mathf.Epsilon)
            {
                return closestPoint;
            }

            Vector3 directionFromCenter = position - transform.position;
            if (directionFromCenter.sqrMagnitude > Mathf.Epsilon)
            {
                float fallbackDistance = Mathf.Max(col.bounds.extents.magnitude, 0.1f);
                return transform.position + directionFromCenter.normalized * fallbackDistance;
            }
        }

        return transform.position;
    }

    public float GetGravityMagnitude(Vector3 position)
    {
        if (!useRadius || effectiveRadius <= 0f)
            return gravityStrength;

        float distance = Vector3.Distance(position, GetGravityPoint(position));
        if (distance >= effectiveRadius)
            return 0f;

        float normalized = Mathf.Clamp01(1f - (distance / effectiveRadius));
        return gravityStrength * Mathf.Max(0.1f, normalized);
    }

    void OnDrawGizmosSelected()
    {
        if (effectiveRadius <= 0f)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, effectiveRadius);
    }
}
