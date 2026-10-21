using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Camera 2.5D: nhìn chéo từ trên xuống (góc cố định) và bám theo nhân vật một cách mượt mà.
    /// Hỗ trợ rung màn hình và chế độ quay vòng quanh làng ở màn hình menu.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float distance = 21f;
        public float pitch = 50f;
        public float yaw = 45f;
        public float smooth = 7f;
        public float zoom = 1f;
        public bool orbit;
        public Vector3 lookOffset = new(0f, 1.2f, 0f);

        public Camera Cam { get; private set; }
        public Vector3 FlatForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 FlatRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 focus;
        float orbitAngle;
        float shakeTime, shakeDuration, shakeMagnitude;
        float currentZoom = 1f;

        void Awake() => Cam = GetComponent<Camera>();

        public void Snap()
        {
            if (target != null) focus = target.position + lookOffset;
        }

        public void Shake(float magnitude, float duration)
        {
            float remaining = shakeDuration > 0f ? shakeMagnitude * (shakeTime / shakeDuration) : 0f;
            if (magnitude < remaining) return;
            shakeMagnitude = magnitude;
            shakeDuration = shakeTime = duration;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            float useYaw = yaw;
            float targetZoom = zoom;

            if (orbit)
            {
                orbitAngle += dt * 5f;
                useYaw = yaw + orbitAngle;
                focus = Vector3.Lerp(focus, new Vector3(0f, 1f, 0f), 1f - Mathf.Exp(-2f * dt));
                targetZoom = 1.35f;
            }
            else
            {
                orbitAngle = 0f;
                if (target != null)
                    focus = Vector3.Lerp(focus, target.position + lookOffset, 1f - Mathf.Exp(-smooth * dt));
            }

            currentZoom = Mathf.Lerp(currentZoom, targetZoom, 1f - Mathf.Exp(-3f * dt));
            Quaternion rot = Quaternion.Euler(pitch, useYaw, 0f);
            Vector3 pos = focus - rot * Vector3.forward * (distance * currentZoom);

            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                float k = shakeMagnitude * Mathf.Clamp01(shakeTime / shakeDuration);
                pos += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * k;
            }

            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
