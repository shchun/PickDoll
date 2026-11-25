using UnityEngine;
namespace PickDoll {
    // Keep strong swing damping; only the cable's vertical twist gets lighter damping.
    public sealed class PassiveTwist : MonoBehaviour {
        public Rigidbody hub;
        public float twistDamping=2.2f;
        private void FixedUpdate(){var vertical=Vector3.Project(hub.angularVelocity,Vector3.up);hub.AddTorque(vertical*Mathf.Max(0,hub.angularDamping-twistDamping),ForceMode.Acceleration);}
    }
}
