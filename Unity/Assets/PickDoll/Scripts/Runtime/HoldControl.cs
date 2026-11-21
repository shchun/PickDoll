using UnityEngine;
using UnityEngine.EventSystems;

namespace PickDoll
{
    public sealed class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public MachineController machine;
        public Vector2 direction;
        public float rotation;
        private int? pointer;
        public void OnPointerDown(PointerEventData data)
        {
            if (pointer.HasValue) return;
            pointer = data.pointerId;
            machine.Hold(this, direction, true); machine.Rotate(this, rotation, true);
        }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) Release(); }
        private void Release()
        {
            if (!pointer.HasValue) return;
            pointer = null;
            if (machine) { machine.Hold(this, direction, false); machine.Rotate(this, rotation, false); }
        }
        private void OnApplicationFocus(bool focus) { if (!focus) Release(); }
        private void OnApplicationPause(bool pause) { if (pause) Release(); }
        private void OnDisable() { Release(); }
    }
}
