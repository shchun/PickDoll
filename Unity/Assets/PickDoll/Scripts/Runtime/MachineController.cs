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
        public float markerHeight=1.116f;
        public float speed = 0.24f;
        public float fineSpeed = 0.035f;
        public float travelLimit = 0.31f;
        public float carrySpeed;
        public float acceleration;
        private Vector2 velocity;
        private Vector2 analog;
        public Vector2 Analog => analog;
        public void SetAnalog(Vector2 value) { analog=Vector2.ClampMagnitude(value,1); }
        private Vector2 autoVelocity;
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
        public void ReleaseControls() { analog=Vector2.zero; held.Clear(); turns.Clear(); pendingDirection = Vector2.zero; velocity = Vector2.zero; autoVelocity = Vector2.zero; }
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
            var cycle = GetComponent<ClawCycle>(); if (cycle && !cycle.CanMove) { velocity = Vector2.zero; AtLimit = false; return; }
            var position = carriage.localPosition;
            direction = Vector2.ClampMagnitude(direction, 1);
            float selectedSpeed = fine ? fineSpeed : cycle && cycle.Phase == ClawCycle.Stage.Carry && carrySpeed > 0 ? carrySpeed : speed;
            var desired = direction * selectedSpeed;
            // Precision and input release stop immediately; only normal starts/reversals ramp.
            velocity = acceleration > 0 && !fine && direction.sqrMagnitude > 0
                ? Vector2.MoveTowards(velocity, desired, acceleration * seconds) : desired;
            var requested = position + new Vector3(velocity.x, 0, velocity.y) * seconds;
            position.x = Mathf.Clamp(requested.x, -travelLimit, travelLimit);
            position.z = Mathf.Clamp(requested.z, -travelLimit, travelLimit);
            AtLimit = position.x != requested.x || position.z != requested.z;
            if (position.x != requested.x) velocity.x = 0;
            if (position.z != requested.z) velocity.y = 0;
            carriage.localPosition = position;
            UpdateMarker();
        }
        internal bool TravelTo(Vector2 target,float maxSpeed,float dt)
        {
            var delta=target-Aim;float distance=delta.magnitude;float accel=Mathf.Max(.1f,acceleration);
            float desiredSpeed=Mathf.Min(maxSpeed,Mathf.Sqrt(2*accel*distance));
            autoVelocity=Vector2.MoveTowards(autoVelocity,delta.normalized*desiredSpeed,accel*dt);
            var next=Aim+autoVelocity*dt;
            bool arrived=distance<=Mathf.Max(.001f,autoVelocity.magnitude*dt);
            if(arrived){next=target;autoVelocity=Vector2.zero;}
            carriage.localPosition=new Vector3(Mathf.Clamp(next.x,-travelLimit,travelLimit),carriage.localPosition.y,Mathf.Clamp(next.y,-travelLimit,travelLimit));
            UpdateMarker();return arrived;
        }
        private void UpdateMarker()
        {
            if (aimMarker) aimMarker.position = new Vector3(carriage.position.x, markerHeight, carriage.position.z);
        }
        private void Update()
        {
            var direction = analog;
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
