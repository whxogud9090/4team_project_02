using UnityEngine;

/// <summary>Pixel-friendly camera follow for the prototype map.</summary>
public sealed class CameraFollow2D : MonoBehaviour
{
    public Transform Target { get; set; }
    [SerializeField] private float followSpeed = 7f;

    private void LateUpdate()
    {
        if (Target == null) return;
        Vector3 desired = new(Target.position.x, Target.position.y, -10f);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }
}
