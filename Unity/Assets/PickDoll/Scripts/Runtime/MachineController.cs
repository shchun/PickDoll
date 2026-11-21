using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace PickDoll
{
    [DefaultExecutionOrder(-100)]
    public sealed class MachineController : MonoBehaviour
    {
        public Transform carriage;
        public Transform claw;
        public Transform aimMarker;
        public float speed = 0.24f;
        public float fineSpeed = 0.035f;
        public float travelLimit = 0.31f;
        public bool Fine { get; private set; }
        public Vector2 Aim => new Vector2(carriage.localPosition.x, carriage.localPosition.z);
        public bool AtLimit { get; private set; }
        private readonly Dictionary<HoldControl, Vector2> held = new Dictionary<HoldControl, Vector2>();
        private readonly Dictionary<HoldControl, float> turns = new Dictionary<HoldControl, float>();
        private float yaw;
        private Vector2 pendingDirection;
        private bool pendingFine;

        public void Hold(HoldControl source, Vector2 direction, bool pressed) { if (pressed) held[source] = direction; else held.Remove(source); }
        public void Rotate(HoldControl source, float direction, bool pressed) { if (GetComponent<ClawCycle>()?.PassiveSuspension == true) return; if (pressed) turns[source] = direction; else turns.Remove(source); }
        public void ToggleFine() { Fine = !Fine; }
        public void ReleaseControls() { held.Clear(); turns.Clear(); pendingDirection = Vector2.zero; }
        public void ResetPosition()
        {
            ReleaseControls(); pendingDirection = Vector2.zero; yaw = 0;
            carriage.localPosition = new Vector3(0, carriage.localPosition.y, 0);
            if (claw) claw.localRotation = Quaternion.identity;
            UpdateMarker();
            var cycle = GetComponent<ClawCycle>(); if (cycle) cycle.ResetCycle();
        }
        public void Move(Vector2 direction, float seconds, bool fine)
        {
            var cycle = GetComponent<ClawCycle>(); if (cycle && !cycle.CanMove) { AtLimit = false; return; }
            var position = carriage.localPosition;
            var requested = position + new Vector3(direction.x, 0, direction.y) * ((fine ? fineSpeed : speed) * seconds);
            position.x = Mathf.Clamp(requested.x, -travelLimit, travelLimit);
            position.z = Mathf.Clamp(requested.z, -travelLimit, travelLimit);
            AtLimit = position.x != requested.x || position.z != requested.z;
            carriage.localPosition = position;
            UpdateMarker();
        }
        private void UpdateMarker()
        {
            if (aimMarker) aimMarker.position = new Vector3(carriage.position.x, 1.116f, carriage.position.z);
        }
        private void Update()
        {
            var direction = Vector2.zero;
            foreach (var value in held.Values) direction += value;
            var turn = 0f;
            foreach (var value in turns.Values) turn += value;
            var keyboard = Keyboard.current;
            bool fine = Fine;
            if (keyboard != null)
            {
                direction.x += (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
                direction.y += (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0);
                turn += (keyboard.xKey.isPressed ? 1 : 0) - (keyboard.zKey.isPressed ? 1 : 0);
                fine |= keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                if (keyboard.rKey.wasPressedThisFrame) ResetPosition();
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) GetComponent<ClawCycle>()?.Press();
            }
            var pickup = GetComponent<ClawCycle>();
            pendingDirection = Vector2.ClampMagnitude(direction, 1); pendingFine = fine;
            if (!pickup || !pickup.PassiveSuspension) Move(pendingDirection, Time.deltaTime, fine);
            if (pickup && pickup.PassiveSuspension) return;
            if (!pickup || !pickup.Busy) yaw += Mathf.Clamp(turn, -1, 1) * 65 * Time.deltaTime;
            if (claw) claw.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        private void FixedUpdate()
        {
            var pickup = GetComponent<ClawCycle>();
            if (pickup && pickup.PassiveSuspension) Move(pendingDirection, Time.fixedDeltaTime, pendingFine);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) ReleaseControls(); }
        private void OnApplicationPause(bool pause) { if (pause) ReleaseControls(); }
        private void OnDisable() { ReleaseControls(); }
    }
}
