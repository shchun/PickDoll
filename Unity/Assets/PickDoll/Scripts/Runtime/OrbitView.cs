using UnityEngine;
using UnityEngine.EventSystems;

namespace PickDoll
{
    public sealed class OrbitView : MonoBehaviour
    {
        public Transform target;
        public float yaw = 12;
        public float elevation = 15;
        public float controlsHeight = 350;
        public float headerHeight = 205;
        public float minimumDistance=3.5f;
        public float framingHeight = 2.65f;
        public float framingWidth = 1.75f;
        public bool pinchZoom;
        public float maxZoom=2;
        [Range(0,.3f)] public float zoomScreenOffset=.15f;
        public float Zoom { get; private set; }=1;
        private float baseFieldOfView;
        public void ZoomByRatio(float ratio) { if(!pinchZoom||float.IsNaN(ratio)||float.IsInfinity(ratio)||ratio<=0)return; Zoom=Mathf.Clamp(Zoom*ratio,1,maxZoom);Apply(); }
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
            Zoom=1;
            preset = (preset + 1) % 4;
            yaw = new[] { 0f, -65f, 65f, 180f }[preset]; elevation = 20;
            Apply();
        }
        public void Apply()
        {
            var camera = GetComponent<Camera>();
            float scale = Mathf.Sqrt((Screen.width / 900f) * (Screen.height / 1600f));
            var safe = Screen.safeArea;
            float bottom = safe.yMin + controlsHeight * scale;
            float top = safe.yMax - headerHeight * scale;
            if (top > bottom + 100) camera.rect = new Rect(safe.xMin / Screen.width, bottom / Screen.height, safe.width / Screen.width, (top - bottom) / Screen.height);
            // Fit cabinet plus breathing room; keep phone and unfolded Fold usable.
            if(baseFieldOfView<=0)baseFieldOfView=camera.fieldOfView;
            float vertical = baseFieldOfView * Mathf.Deg2Rad;
            camera.fieldOfView=2*Mathf.Atan(Mathf.Tan(vertical/2)/Zoom)*Mathf.Rad2Deg;
            float fitHeight = framingHeight / (2 * Mathf.Tan(vertical / 2));
            float fitWidth = framingWidth / (2 * Mathf.Tan(vertical / 2) * camera.aspect);
            distance = Mathf.Max(minimumDistance, fitHeight, fitWidth);
            var rotation = Quaternion.Euler(elevation, yaw, 0);
            transform.position = target.position + rotation * new Vector3(0, 0, -distance);
            transform.LookAt(target);
            // Move the enlarged scene down in the game viewport, cropping the cabinet controls.
            // Keep the orbit pivot fixed so dragging and machine movement keep their existing meaning.
            camera.ResetProjectionMatrix();
            var projection=camera.projectionMatrix;
            projection[1,2]+=2*zoomScreenOffset*Mathf.InverseLerp(1,maxZoom,Zoom);
            camera.projectionMatrix=projection;
        }
        private void LateUpdate() { Apply(); }
        private void OnDisable(){var camera=GetComponent<Camera>();if(camera)camera.ResetProjectionMatrix();}
    }

    public sealed class OrbitDragArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
    {
        public OrbitView orbit;
        private readonly System.Collections.Generic.Dictionary<int,Vector2> touches = new System.Collections.Generic.Dictionary<int,Vector2>();
        private bool suppressSingle;
        public int TouchCount => touches.Count;
        private float Span() { var points=new System.Collections.Generic.List<Vector2>(touches.Values);return Vector2.Distance(points[0],points[1]); }
        public void OnPointerDown(PointerEventData data) {
            if(touches.Count>=2)return;
            touches[data.pointerId]=data.position;suppressSingle=touches.Count==2;
        }
        public void OnDrag(PointerEventData data) {
            if(!touches.TryGetValue(data.pointerId,out var previous))return;
            float oldSpan=touches.Count==2?Span():0;
            touches[data.pointerId]=data.position;
            if(touches.Count==2) { if(oldSpan>5&&Span()>5)orbit.ZoomByRatio(Span()/oldSpan); }
            else if(suppressSingle)suppressSingle=false;
            else orbit.Drag(data.position-previous);
        }
        public void OnPointerUp(PointerEventData data) { if(touches.Remove(data.pointerId))suppressSingle=true; }
        public void OnScroll(PointerEventData data) { orbit.ZoomByRatio(Mathf.Pow(1.12f,Mathf.Clamp(data.scrollDelta.y,-1,1))); }
        public void CancelTouches(){touches.Clear();suppressSingle=false;}
        private void OnApplicationPause(bool paused) { if(paused)CancelTouches(); }
        private void OnApplicationFocus(bool focused) { if(!focused)CancelTouches(); }
        private void OnDisable()=>CancelTouches();
    }
}
