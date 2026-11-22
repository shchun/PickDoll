using System;
using System.Collections.Generic;
using UnityEngine;
namespace PickDoll
{
    public sealed class PrizeReceiver : MonoBehaviour
    {
        public Rigidbody[] prizes;
        public BoxCollider entryZone, exitZone;
        public Vector2 holeCenter = new Vector2(-.23f,-.22f);
        public Vector2 holeSize = new Vector2(.36f,.36f);
        public float outsideFaceZ = -.50f;
        private readonly HashSet<string> receipts = new HashSet<string>();
        private bool initialized;
        public int Count => receipts.Count;
        public event Action<PrizeIdentity> Acquired;
        public void Initialize(){if(initialized)return;initialized=true;foreach(var body in prizes)body.GetComponent<PrizeIdentity>().EnsureTicket();}
        private void Awake()=>Initialize();
        public void BeginNewRound(){Initialize();foreach(var body in prizes)body.GetComponent<PrizeIdentity>().Rearm();}
        public bool OverHole(Vector2 aim,float margin=.025f)=>Mathf.Abs(aim.x-holeCenter.x)<holeSize.x*.5f+margin&&Mathf.Abs(aim.y-holeCenter.y)<holeSize.y*.5f+margin;
        public void Probe()
        {
            Initialize();
            foreach(var body in prizes) {
                var prize=body.GetComponent<PrizeIdentity>();if(prize.Delivered||!body.gameObject.activeInHierarchy)continue;
                var bounds=new Bounds();bool first=true;
                foreach(var collider in body.GetComponentsInChildren<Collider>()) {
                    if(collider.isTrigger)continue;
                    if(first){bounds=collider.bounds;first=false;}else bounds.Encapsulate(collider.bounds);
                }
                if(first)continue;
                if(!prize.EnteredChute && entryZone.bounds.Intersects(bounds)) {
                    prize.EnteredChute=true;
                    if(Debug.isDebugBuild)Debug.Log($"PICKDOLL_CHUTE_ENTRY {prize.displayName} at={body.position}");
                }
                if(!prize.EnteredChute||!exitZone.bounds.Intersects(bounds))continue;
                // The whole prize must clear the front face, not just touch a trigger with one foot.
                if(first||bounds.max.z>=outsideFaceZ)continue;
                prize.Delivered=true;
                if(receipts.Add(prize.Ticket)) {
                    if(Debug.isDebugBuild)Debug.Log($"PICKDOLL_ACQUIRED count={Count} prize={prize.displayName} at={body.position}");
                    Acquired?.Invoke(prize);
                }
            }
        }
        private void FixedUpdate()=>Probe();
    }
}
