using UnityEngine;
using UnityEngine.EventSystems;
namespace PickDoll
{
    public sealed class AnalogJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MachineController machine;
        public RectTransform knob;
        public float radius=82;
        private int? pointer;
        public static Vector2 Response(Vector2 value) {
            float amount=Mathf.Clamp01((value.magnitude-.06f)/.94f);
            return value.normalized*amount*amount;
        }
        public void OnPointerDown(PointerEventData data) {
            if(pointer.HasValue||!machine.GetComponent<ClawCycle>().CanMove)return;
            pointer=data.pointerId;OnDrag(data);
        }
        public void OnDrag(PointerEventData data) {
            if(pointer!=data.pointerId)return;
            if(!machine.GetComponent<ClawCycle>().CanMove){CancelTouch();return;}
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var local))return;
            var offset=Vector2.ClampMagnitude(local/radius,1);
            knob.anchoredPosition=offset*radius;
            machine.SetAnalog(Response(offset));
        }
        public void OnPointerUp(PointerEventData data) { if(pointer==data.pointerId)CancelTouch(); }
        public void CancelTouch(){pointer=null;if(knob)knob.anchoredPosition=Vector2.zero;if(machine)machine.SetAnalog(Vector2.zero);}
        private void Update(){if(pointer.HasValue&&!machine.GetComponent<ClawCycle>().CanMove)CancelTouch();}
        private void OnApplicationPause(bool paused){if(paused)CancelTouch();}
        private void OnApplicationFocus(bool focus){if(!focus)CancelTouch();}
        private void OnDisable()=>CancelTouch();
    }
}
