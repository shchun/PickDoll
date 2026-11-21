using UnityEngine;
using UnityEngine.EventSystems;

namespace PickDoll
{
    public sealed class OrbitView : MonoBehaviour
    {
        public Transform target;
        public float yaw = 12;
        public float elevation = 15;
        private float distance = 3.9f;
        private int preset;
        public void Drag(Vector2 delta)
        {
            yaw -= delta.x * 0.18f;
            elevation = Mathf.Clamp(elevation - delta.y * 0.14f, 8, 78);
            Apply();
        }
        public void NextPreset()
        {
            preset = (preset + 1) % 4;
            yaw = new[] { 0f, -65f, 65f, 180f }[preset]; elevation = 20;
            Apply();
        }
        public void Apply()
        {
            var camera = GetComponent<Camera>();
            float scale = Mathf.Sqrt((Screen.width / 900f) * (Screen.height / 1600f));
            var safe = Screen.safeArea;
            float bottom = safe.yMin + 350 * scale;
            float top = safe.yMax - 205 * scale;
            if (top > bottom + 100) camera.rect = new Rect(safe.xMin / Screen.width, bottom / Screen.height, safe.width / Screen.width, (top - bottom) / Screen.height);
            // Fit cabinet plus breathing room; keep phone and unfolded Fold usable.
            float vertical = camera.fieldOfView * Mathf.Deg2Rad;
            float fitHeight = 2.65f / (2 * Mathf.Tan(vertical / 2));
            float fitWidth = 1.75f / (2 * Mathf.Tan(vertical / 2) * camera.aspect);
            distance = Mathf.Max(3.5f, fitHeight, fitWidth);
            var rotation = Quaternion.Euler(elevation, yaw, 0);
            transform.position = target.position + rotation * new Vector3(0, 0, -distance);
            transform.LookAt(target);
        }
        private void LateUpdate() { Apply(); }
    }

    public sealed class OrbitDragArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public OrbitView orbit;
        private int? pointer;
        public void OnPointerDown(PointerEventData data) { if (!pointer.HasValue) pointer = data.pointerId; }
        public void OnDrag(PointerEventData data) { if (pointer == data.pointerId) orbit.Drag(data.delta); }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) pointer = null; }
        private void OnDisable() { pointer = null; }
    }
}
