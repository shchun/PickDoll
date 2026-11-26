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
        public Collider pickupFloor;
        private readonly HashSet<string> receipts = new HashSet<string>();
        private bool initialized;
        public bool persistentProgress;
        public CollectionStore Store {get;private set;}
        public int Count => Store!=null?Store.Total:receipts.Count;
        public event Action<PrizeIdentity> Acquired;
        public event Action RoundStarted;
        public void Initialize(){if(initialized)return;initialized=true;if(persistentProgress)Store=new CollectionStore(CollectionStore.DefaultPath);foreach(var body in prizes)body.GetComponent<PrizeIdentity>().EnsureTicket();}
        private void Awake()=>Initialize();
        public void BeginNewRound(){Initialize();foreach(var body in prizes)body.GetComponent<PrizeIdentity>().Rearm();RoundStarted?.Invoke();}
        public bool OverHole(Vector2 aim,float margin=.025f)=>Mathf.Abs(aim.x-holeCenter.x)<holeSize.x*.5f+margin&&Mathf.Abs(aim.y-holeCenter.y)<holeSize.y*.5f+margin;
        public void TouchPickupFloor(PrizeIdentity prize)
        {
            Initialize();
            if(!prize || prize.Delivered || !prize.EnteredChute)return;
            bool added;
            if(Store!=null){if(!Store.Commit(prize.Ticket,prize.SpeciesId,out added))return;}
            else added=receipts.Add(prize.Ticket);
            prize.Delivered=true;
            if(added){Debug.Log($"PICKDOLL_ACQUIRED count={Count} prize={prize.displayName} floor_contact=true");Acquired?.Invoke(prize);}
        }
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
                if(pickupFloor || !prize.EnteredChute||!exitZone.bounds.Intersects(bounds))continue;
                // The whole prize must clear the front face, not just touch a trigger with one foot.
                if(first||bounds.max.z>=outsideFaceZ)continue;
                bool added;
                if(Store!=null){if(!Store.Commit(prize.Ticket,prize.SpeciesId,out added))continue;}
                else added=receipts.Add(prize.Ticket);
                prize.Delivered=true;
                if(added) {
                    if(Debug.isDebugBuild)Debug.Log($"PICKDOLL_ACQUIRED count={Count} prize={prize.displayName} at={body.position}");
                    Acquired?.Invoke(prize);
                }
            }
        }
        private void FixedUpdate()=>Probe();
    }
}
