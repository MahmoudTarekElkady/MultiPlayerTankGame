using UnityEngine;

public class CameraFollowTopDown : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 20f, 0f);

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Look straight down
    }
}
