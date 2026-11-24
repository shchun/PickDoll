using UnityEngine;

namespace PickDoll
{
    public sealed class ClawCycle : MonoBehaviour
    {
        public enum Stage { Ready, Lowering, Closing, Raising, Carry, Opening, Returning, Settling, Repositioning, Preparing }
        public MachineController machine;
        public Rigidbody hub;
        public HingeJoint[] fingers;
        public Rigidbody[] prizes;
        public PrizeStock stock;
        public Transform cable;
        public Rigidbody suspensionAnchor;
        public float suspensionLength = .14f;
        public bool PassiveSuspension => suspensionAnchor != null;
        public bool CanMove => !Busy || (PassiveSuspension && (Phase == Stage.Lowering || Phase == Stage.Preparing));
        public float topHeight = 1.83f;
        public float bottomHeight = 1.42f;
        public float gripStiffness = 1.5f;
        public float gripDamping = .06f;
        public float closeAngle = -37;
        public float releaseAngle;
        public PrizeReceiver receiver;
        public bool automaticDelivery;
        public float returnSpeed=.48f;
        public bool contactDescent;
        public ClawContactProbe[] contactProbes;
        public float contactForce=1.2f,contactDuration=.12f,hardContactForce=4.5f,descentTimeout=4.5f;
        private float contactTime;
        public string DescentStop {get;private set;}="";
        public float PrizeContactForce {get;private set;}
        public float StructureContactForce {get;private set;}
        private void ClearContact(){contactTime=0;DescentStop="";PrizeContactForce=StructureContactForce=0;if(contactProbes!=null)foreach(var probe in contactProbes)probe.Clear();}

        public float openingTolerance=6;
        private float settledTime;
        private bool needsOpenSpace;
        public int OpenFingerCount { get {
            if(!initialized)return 0;int count=0;
            for(int i=0;i<fingers.Length;i++) {
                var open=fingerRotations[i];var local=Quaternion.Inverse(hub.rotation)*fingers[i].transform.rotation;
                float angle=Vector3.SignedAngle(open*Vector3.down,local*Vector3.down,open*Vector3.forward);
                if(angle>=-openingTolerance)count++;
            }
            return count;
        } }
        public bool CanPress => !Busy && (!automaticDelivery || Phase!=Stage.Ready || !receiver.OverHole(machine.Aim));
        public bool smoothMotion;
        public float loweringSpeed = .28f;
        public float raisingSpeed = .24f;
        public float verticalAcceleration = .9f;
        public float closingRamp = .8f;
        public float closingDuration = 1.05f;
        public bool gripVariationEnabled;
        [Range(0,1)] public float weakGripChance = .25f;
        public Vector2 weakGripRange = new Vector2(.4f,.65f);
        public Vector2 normalGripRange = new Vector2(.95f,1.05f);
        public float CurrentGripScale { get; private set; } = 1;
        public bool CurrentWeakGrip { get; private set; }
        private float verticalVelocity;
        public Stage Phase { get; private set; }
        public bool Busy => Phase != Stage.Ready && Phase != Stage.Carry;
        public string Status => Phase switch {
            Stage.Lowering => PassiveSuspension ? "LOWERING / MOVE TO ADJUST" : "LOWERING", Stage.Closing => "CLOSING",
            Stage.Raising => "RAISING", Stage.Carry => "MOVE TO CARRY / OPEN TO RELEASE",
            Stage.Preparing => "OPENING BEFORE LOWERING", Stage.Returning => "RETURNING TO CHUTE",
            Stage.Settling => "SETTLING BEFORE RELEASE", Stage.Repositioning => "RETURNING TO PLAY",
            Stage.Opening => automaticDelivery ? "RELEASING" : "OPENING", _ => needsOpenSpace ? "MOVE TO GIVE CLAW SPACE" : CanPress ? "AIM, THEN GRAB" : "MOVE OUT OF CHUTE AREA"
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
            if(receiver)receiver.Initialize();
            height = topHeight; Phase = Stage.Ready;
            SetSpring(0);
            if(stock)stock.Restock();
        }
        private void Awake() { if (PassiveSuspension) Time.fixedDeltaTime = .01f; Initialize(); }
        public void Press()
        {
            Initialize();
            if (Phase == Stage.Ready && CanPress) {
                float category = gripVariationEnabled ? Random.value : 1;
                CurrentWeakGrip = gripVariationEnabled && (weakGripChance >= 1 || category < weakGripChance);
                CurrentGripScale = EvaluateGrip(category, gripVariationEnabled ? Random.value : .5f);
                ClearContact();elapsed = 0; verticalVelocity = 0; SetSpring(0); needsOpenSpace=false; Phase = automaticDelivery ? Stage.Preparing : Stage.Lowering; if (!PassiveSuspension) machine.ReleaseControls(); }
            else if (Phase == Stage.Carry) { elapsed = 0; Phase = Stage.Opening; }
        }
        public void ResetCycle()
        {
            Initialize(); if(receiver)receiver.BeginNewRound(); settledTime=0; needsOpenSpace=false; height = topHeight; verticalVelocity = 0; elapsed = 0; CurrentGripScale = 1; CurrentWeakGrip = false; Phase = Stage.Ready; SetSpring(0);
            var target = new Vector3(machine.carriage.position.x, height, machine.carriage.position.z);
            hub.position = target; hub.rotation = machine.claw.rotation;
            if (PassiveSuspension) {
                suspensionAnchor.position = target + Vector3.up * suspensionLength;
                hub.linearVelocity = Vector3.zero; hub.angularVelocity = Vector3.zero;
            }
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
                if (!prizes[i].isKinematic) { prizes[i].linearVelocity = Vector3.zero; prizes[i].angularVelocity = Vector3.zero; prizes[i].WakeUp(); }
            }
            if(stock)stock.Restock();
            ClearContact();
            Physics.SyncTransforms();
        }
        private void SetSpring(float target)
        {
            foreach (var finger in fingers)
            {
                float forceScale = Phase == Stage.Closing || Phase == Stage.Raising || Phase == Stage.Carry || Phase == Stage.Returning || Phase == Stage.Settling ? CurrentGripScale : 1;
                var spring = finger.spring; spring.spring = gripStiffness * forceScale; spring.damper = gripDamping * Mathf.Sqrt(forceScale);
                spring.targetPosition = target; finger.spring = spring; finger.useSpring = true;
            }
        }
        public float EvaluateGrip(float categoryRoll, float strengthRoll)
        {
            if (!gripVariationEnabled) return 1;
            var range = (weakGripChance >= 1 || categoryRoll < weakGripChance) ? weakGripRange : normalGripRange;
            return Mathf.Max(.01f, Mathf.Lerp(range.x, range.y, Mathf.Clamp01(strengthRoll)));
        }
        private void MoveLift(float target, float speed, float dt)
        {
            if (!smoothMotion) { height = Mathf.MoveTowards(height, target, speed * dt); return; }
            float accel = Mathf.Max(.01f, verticalAcceleration);
            float approachSpeed = Mathf.Min(speed, Mathf.Sqrt(2 * accel * Mathf.Abs(target-height)));
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, Mathf.Sign(target-height) * approachSpeed, accel * dt);
            if (Mathf.Abs(target-height) <= Mathf.Abs(verticalVelocity*dt)) { height = target; verticalVelocity = 0; }
            else height += verticalVelocity * dt;
        }
        public void Step(float dt)
        {
            Initialize(); elapsed += dt;
            if(needsOpenSpace&&OpenFingerCount>=2)needsOpenSpace=false;
            switch (Phase)
            {
                case Stage.Preparing:
                    SetSpring(0);
                    if(elapsed>=.55f&&OpenFingerCount>=2){Phase=Stage.Lowering;elapsed=0;}
                    else if(elapsed>=1.8f){needsOpenSpace=true;Phase=Stage.Ready;elapsed=0;machine.ReleaseControls();}
                    break;
                case Stage.Lowering:
                    if(automaticDelivery)SetSpring(0);
                    float speed=loweringSpeed;bool stop=false;
                    if(contactDescent) {
                        PrizeContactForce=StructureContactForce=0;
                        foreach(var probe in contactProbes){probe.Read(out var p,out var s);PrizeContactForce+=p;StructureContactForce+=s;}
                        contactTime=PrizeContactForce>=contactForce?contactTime+dt:0;
                        if(PrizeContactForce>.15f)speed=Mathf.Min(speed,.075f);
                        if(StructureContactForce>=contactForce){DescentStop="STRUCTURE";stop=true;}
                        else if(PrizeContactForce>=hardContactForce||contactTime>=contactDuration){DescentStop="PRIZE_CONTACT";stop=true;}
                        else if(elapsed>=descentTimeout){DescentStop="TIME_LIMIT";stop=true;}
                    }
                    if(!stop)MoveLift(bottomHeight,speed,dt);
                    if(height<=bottomHeight){DescentStop="DEPTH_LIMIT";stop=true;}
                    if(stop){verticalVelocity=0;Phase=Stage.Closing;elapsed=0;}
                    break;
                case Stage.Closing:
                    float progress = Mathf.Clamp01(elapsed / closingRamp);
                    SetSpring(Mathf.Lerp(0, closeAngle, smoothMotion ? Mathf.SmoothStep(0,1,progress) : progress));
                    if (elapsed >= closingDuration) { Phase = Stage.Raising; elapsed = 0; }
                    break;
                case Stage.Raising:
                    MoveLift(topHeight, raisingSpeed, dt);
                    if (height >= topHeight) { Phase = automaticDelivery ? Stage.Returning : Stage.Carry; elapsed = 0; machine.ReleaseControls(); }
                    break;
                case Stage.Returning:
                    if(machine.TravelTo(receiver.holeCenter,returnSpeed,dt)){Phase=Stage.Settling;elapsed=0;settledTime=0;}
                    break;
                case Stage.Settling:
                    var horizontal=new Vector2(hub.position.x,hub.position.z);
                    var swayVelocity=new Vector2(hub.linearVelocity.x,hub.linearVelocity.z);
                    bool stable=Vector2.Distance(horizontal,receiver.holeCenter)<.035f && swayVelocity.magnitude<.15f;
                    settledTime=stable?settledTime+dt:0;
                    if((elapsed>=.45f&&settledTime>=.15f)||elapsed>=1.8f){Phase=Stage.Opening;elapsed=0;}
                    break;
                case Stage.Repositioning:
                    MoveLift(topHeight,raisingSpeed,dt);
                    bool home=machine.TravelTo(machine.homePosition,returnSpeed,dt);
                    if(home&&height>=topHeight){Phase=Stage.Ready;elapsed=0;machine.ReleaseControls();}
                    break;
                case Stage.Opening:
                    SetSpring(Mathf.Lerp(closeAngle, releaseAngle, Mathf.Clamp01(elapsed / .55f)));
                    if (elapsed >= (PassiveSuspension ? 1.1f : .7f)) { SetSpring(0); Phase = automaticDelivery ? Stage.Repositioning : Stage.Ready; elapsed = 0; machine.ReleaseControls(); }
                    break;
            }
            var target = new Vector3(machine.carriage.position.x, height, machine.carriage.position.z);
            if (PassiveSuspension) suspensionAnchor.MovePosition(target + Vector3.up * suspensionLength);
            else { hub.MovePosition(target); hub.MoveRotation(machine.claw.rotation); }
        }
        private void FixedUpdate() { if (!paused) Step(Time.fixedDeltaTime); }
        private void LateUpdate()
        {
            if (!cable) return;
            var top = machine.carriage.position - Vector3.up * .02f;
            var bottom = hub.position + Vector3.up * .02f;
            cable.position = (top + bottom) * .5f;
            cable.rotation = Quaternion.FromToRotation(Vector3.up, top - bottom);
            cable.localScale = new Vector3(.009f, Mathf.Max(.01f, Vector3.Distance(top, bottom) * .5f), .009f);
        }
        private void OnApplicationPause(bool value) { paused = value; if (value) machine.ReleaseControls(); }
        private void OnApplicationFocus(bool value) { if (!value) machine.ReleaseControls(); }
    }
}
