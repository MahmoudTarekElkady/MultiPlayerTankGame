using UnityEngine;

public class CameraFollowTopDown : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 10f, -7f); // adjust as needed
    public float followSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f); // adjust tilt/look offset
    }
}
