using System.Text;
using UnityEngine;

namespace PickDoll
{
    public sealed class ClawDiagnostics : MonoBehaviour
    {
        public ClawCycle cycle;
        ClawCycle.Stage previous;
        int round;
        float nextCheck, nextAnomaly;
        float nextProjection;
        public void RecordProjection(Joint joint, float error)
        {
            if (error < .015f || Time.realtimeSinceStartup < nextProjection) return;
            nextProjection = Time.realtimeSinceStartup + 5;
            LocalDiagnostics.Write("anchor_projection", "finger=" + joint.name + " before_m=" + error.ToString("F4") + " " + Snapshot());
        }
        public float MaximumAnchorError { get; private set; }
        void Start()
        {
            previous = cycle.Phase;
            LocalDiagnostics.Write("session_start", "version=" + Application.version + " platform=" + Application.platform);
            cycle.receiver.Acquired += Acquired;
            cycle.receiver.RoundStarted += Round;
        }
        void OnDestroy()
        {
            if (!cycle || !cycle.receiver) return;
            cycle.receiver.Acquired -= Acquired;
            cycle.receiver.RoundStarted -= Round;
        }
        void Round() { round++; LocalDiagnostics.Write("round", Snapshot()); }
        void Acquired(PrizeIdentity prize) => LocalDiagnostics.Write("acquired", "species=" + prize.SpeciesId + " ticket=" + prize.Ticket + " " + Snapshot());
        public static float AnchorError(Joint joint)
        {
            if (!joint || !joint.connectedBody) return float.PositiveInfinity;
            var body = joint.GetComponent<Rigidbody>();
            var own = body.position + body.rotation * Vector3.Scale(joint.anchor, body.transform.lossyScale);
            var other = joint.connectedBody;
            var connected = other.position + other.rotation * Vector3.Scale(joint.connectedAnchor, other.transform.lossyScale);
            return Vector3.Distance(own, connected);
        }
        public void Sample()
        {
            float worst = 0;
            foreach (var finger in cycle.ActiveFingers) worst = Mathf.Max(worst, AnchorError(finger));
            MaximumAnchorError = Mathf.Max(MaximumAnchorError, worst);
            if (worst > .015f && Time.realtimeSinceStartup >= nextAnomaly)
            {
                nextAnomaly = Time.realtimeSinceStartup + 5;
                LocalDiagnostics.Write("joint_anomaly", Snapshot());
            }
        }
        string Snapshot()
        {
            var text = new StringBuilder("round=" + round + " phase=" + cycle.Phase + " grip=" + cycle.CurrentGripScale.ToString("F2") +
                " hub=" + cycle.hub.position.ToString("F4") + " velocity=" + cycle.hub.linearVelocity.ToString("F4"));
            for (int i = 0; i < cycle.ActiveFingers.Length; i++)
            {
                var joint = cycle.ActiveFingers[i];
                text.Append(" finger" + i + "=" + (joint ? "alive" : "missing") + " error=" + AnchorError(joint).ToString("F4"));
                if (joint) { var body = joint.GetComponent<Rigidbody>(); text.Append(" pos=" + body.position.ToString("F4") + " velocity=" + body.linearVelocity.ToString("F4") + " angular=" + body.angularVelocity.ToString("F4")); }
            }
            foreach (var body in cycle.prizes) { var prize = body.GetComponent<PrizeIdentity>(); text.Append(" prize=" + prize.SpeciesId + ":" + prize.Ticket + ":" + prize.Delivered + ":" + body.position.ToString("F3")); }
            return text.ToString();
        }
        void FixedUpdate()
        {
            if (previous != cycle.Phase) { LocalDiagnostics.Write("phase", "from=" + previous + " " + Snapshot()); previous = cycle.Phase; }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .1f;
            Sample();
        }
        void OnApplicationPause(bool value) => LocalDiagnostics.Write("pause", "value=" + value + " " + Snapshot());
    }
}
