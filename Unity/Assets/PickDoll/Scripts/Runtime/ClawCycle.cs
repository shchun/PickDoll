using UnityEngine;

namespace PickDoll
{
    public sealed class ClawCycle : MonoBehaviour
    {
        public enum Stage { Ready, Lowering, Closing, Raising, Carry, Opening }
        public MachineController machine;
        public Rigidbody hub;
        public HingeJoint[] fingers;
        public Rigidbody[] prizes;
        public Transform cable;
        public float topHeight = 1.83f;
        public float bottomHeight = 1.42f;
        public float gripStiffness = 1.5f;
        public float gripDamping = .06f;
        public float closeAngle = -37;
        public Stage Phase { get; private set; }
        public bool Busy => Phase != Stage.Ready && Phase != Stage.Carry;
        public string Status => Phase switch {
            Stage.Lowering => "LOWERING", Stage.Closing => "CLOSING",
            Stage.Raising => "RAISING", Stage.Carry => "MOVE TO CARRY / OPEN TO RELEASE",
            Stage.Opening => "OPENING", _ => "AIM, THEN GRAB"
        };
        private Vector3[] prizePositions;
        private Quaternion[] prizeRotations, fingerRotations;
        private float height, elapsed;
        private bool initialized, paused;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            prizePositions = new Vector3[prizes.Length]; prizeRotations = new Quaternion[prizes.Length];
            for (int i = 0; i < prizes.Length; i++) { prizePositions[i] = prizes[i].position; prizeRotations[i] = prizes[i].rotation; }
            fingerRotations = new Quaternion[fingers.Length];
            for (int i = 0; i < fingers.Length; i++) fingerRotations[i] = Quaternion.Inverse(hub.rotation) * fingers[i].transform.rotation;
            for (int a = 0; a < fingers.Length; a++) for (int b = a + 1; b < fingers.Length; b++)
                foreach (var x in fingers[a].GetComponentsInChildren<Collider>()) foreach (var y in fingers[b].GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(x, y);
            height = topHeight; Phase = Stage.Ready;
            SetSpring(0);
        }
        private void Awake() { Initialize(); }
        public void Press()
        {
            Initialize();
            if (Phase == Stage.Ready) { elapsed = 0; Phase = Stage.Lowering; machine.ReleaseControls(); }
            else if (Phase == Stage.Carry) { elapsed = 0; Phase = Stage.Opening; }
        }
        public void ResetCycle()
        {
            Initialize(); height = topHeight; elapsed = 0; Phase = Stage.Ready; SetSpring(0);
            var target = new Vector3(machine.carriage.position.x, height, machine.carriage.position.z);
            hub.position = target; hub.rotation = machine.claw.rotation;
            for (int i = 0; i < fingers.Length; i++)
            {
                var body = fingers[i].GetComponent<Rigidbody>();
                body.position = hub.position + hub.rotation * fingers[i].connectedAnchor;
                body.rotation = hub.rotation * fingerRotations[i];
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.WakeUp();
            }
            for (int i = 0; i < prizes.Length; i++)
            {
                prizes[i].position = prizePositions[i]; prizes[i].rotation = prizeRotations[i];
                prizes[i].linearVelocity = Vector3.zero; prizes[i].angularVelocity = Vector3.zero; prizes[i].WakeUp();
            }
            Physics.SyncTransforms();
        }
        private void SetSpring(float target)
        {
            foreach (var finger in fingers)
            {
                var spring = finger.spring; spring.spring = gripStiffness; spring.damper = gripDamping;
                spring.targetPosition = target; finger.spring = spring; finger.useSpring = true;
            }
        }
        public void Step(float dt)
        {
            Initialize(); elapsed += dt;
            switch (Phase)
            {
                case Stage.Lowering:
                    height = Mathf.MoveTowards(height, bottomHeight, .28f * dt);
                    if (height <= bottomHeight) { Phase = Stage.Closing; elapsed = 0; }
                    break;
                case Stage.Closing:
                    SetSpring(Mathf.Lerp(0, closeAngle, Mathf.Clamp01(elapsed / .8f)));
                    if (elapsed >= 1.05f) { Phase = Stage.Raising; elapsed = 0; }
                    break;
                case Stage.Raising:
                    height = Mathf.MoveTowards(height, topHeight, .24f * dt);
                    if (height >= topHeight) { Phase = Stage.Carry; elapsed = 0; machine.ReleaseControls(); }
                    break;
                case Stage.Opening:
                    SetSpring(Mathf.Lerp(closeAngle, 0, Mathf.Clamp01(elapsed / .55f)));
                    if (elapsed >= .7f) { Phase = Stage.Ready; elapsed = 0; }
                    break;
            }
            hub.MovePosition(new Vector3(machine.carriage.position.x, height, machine.carriage.position.z));
            hub.MoveRotation(machine.claw.rotation);
        }
        private void FixedUpdate() { if (!paused) Step(Time.fixedDeltaTime); }
        private void LateUpdate()
        {
            if (!cable) return;
            var top = machine.carriage.position - Vector3.up * .02f;
            var bottom = hub.position + Vector3.up * .02f;
            cable.position = (top + bottom) * .5f;
            cable.localScale = new Vector3(.009f, Mathf.Max(.01f, Vector3.Distance(top, bottom) * .5f), .009f);
        }
        private void OnApplicationPause(bool value) { paused = value; if (value) machine.ReleaseControls(); }
        private void OnApplicationFocus(bool value) { if (!value) machine.ReleaseControls(); }
    }
}
