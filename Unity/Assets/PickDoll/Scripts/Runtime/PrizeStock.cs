using System;
using System.Collections.Generic;
using UnityEngine;
namespace PickDoll
{
    public sealed class PrizeStock : MonoBehaviour
    {
        public Rigidbody[] prizes;
        public PrizeReceiver receiver;
        public float spawnY=.727f;
        public bool naturalArrangement;
        public struct Pose {public Vector3 position;public Quaternion rotation;}
        public void Restock() {
            if(naturalArrangement){ApplyNatural(Environment.TickCount);return;}
            var slots=Layout(Environment.TickCount,prizes.Length);
            for(int i=0;i<prizes.Length;i++) {
                var body=prizes[i];body.gameObject.SetActive(true);body.isKinematic=false;
                body.position=new Vector3(slots[i].x,spawnY,slots[i].y);body.rotation=Quaternion.Euler(0,UnityEngine.Random.Range(-35f,35f),0);
                body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
                body.GetComponent<PrizeIdentity>().Rearm();body.WakeUp();
            }
            Physics.SyncTransforms();
        }
        public static Bounds BoundsAt(Rigidbody body,Vector3 position,Quaternion rotation) {
            var result=new Bounds();bool first=true;
            foreach(var c in body.GetComponents<Collider>()) {
                Bounds b;
                if(c is SphereCollider sphere)b=new Bounds(position+rotation*sphere.center,Vector3.one*sphere.radius*2);
                else if(c is CapsuleCollider capsule){var axis=rotation*(capsule.direction==0?Vector3.right:capsule.direction==1?Vector3.up:Vector3.forward);axis=new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z));var extents=axis*Mathf.Max(0,capsule.height*.5f-capsule.radius)+Vector3.one*capsule.radius;b=new Bounds(position+rotation*capsule.center,extents*2);}
                else continue;
                if(first){result=b;first=false;}else result.Encapsulate(b);
            }
            return result;
        }
        public Pose[] NaturalLayout(int seed) {
            var random=new System.Random(seed);var poses=new Pose[prizes.Length];
            var occupied=new List<Bounds>();
            for(int restart=0;restart<32;restart++) {
                occupied.Clear();bool complete=true;
                for(int i=0;i<prizes.Length;i++) {
                    bool placed=false;
                    for(int attempt=0;attempt<200;attempt++) {
                        float yaw=(float)random.NextDouble()*360;
                        var rotation=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(i==0||i==3||i==6?90:i==2?18:0,0,i==4?-14:0);
                        var origin=BoundsAt(prizes[i],Vector3.zero,rotation);
                        var position=new Vector3((float)random.NextDouble()*.62f-.31f,.645f-origin.min.y,(float)random.NextDouble()*.62f-.31f);
                        if(i==prizes.Length-1&&occupied.Count>0)position=new Vector3(poses[0].position.x+.015f,occupied[0].max.y-origin.min.y+.025f,poses[0].position.z+.015f);
                        var bounds=BoundsAt(prizes[i],position,rotation);
                        if(bounds.min.x<-.43f||bounds.max.x>.43f||bounds.min.z<-.43f||bounds.max.z>.43f)continue;
                        if(bounds.min.x<-.025f&&bounds.max.x>-.435f&&bounds.min.z<.005f&&bounds.max.z>-.425f)continue;
                        bool overlaps=false;foreach(var other in occupied){var padded=other;padded.Expand(.012f);if(padded.Intersects(bounds)){overlaps=true;break;}}
                        if(overlaps)continue;
                        poses[i]=new Pose {position=position,rotation=rotation};occupied.Add(bounds);placed=true;break;
                    }
                    if(!placed){complete=false;break;}
                }
                if(complete)return poses;
            }
            throw new InvalidOperationException("Cannot place natural stock safely");
        }
        public void ApplyNatural(int seed) {
            var poses=NaturalLayout(seed);
            for(int i=0;i<prizes.Length;i++) {
                var body=prizes[i];body.gameObject.SetActive(true);body.isKinematic=false;
                body.transform.SetPositionAndRotation(poses[i].position,poses[i].rotation);body.position=poses[i].position;body.rotation=poses[i].rotation;
                body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.GetComponent<PrizeIdentity>().Rearm();body.WakeUp();
            }
            Physics.SyncTransforms();
        }
        public static Vector2[] Layout(int seed,int count) {
            // Shuffled, jittered cells guarantee clearance even when rejection sampling would jam.
            // The lower-left four cells are reserved for the guarded chute.
            var random=new System.Random(seed);var candidates=new List<Vector2>();
            foreach(float x in new[]{-.30f,-.10f,.10f,.30f})foreach(float z in new[]{-.30f,-.10f,.10f,.30f}) {
                if(x<0&&z<0)continue;
                candidates.Add(new Vector2(x,z));
            }
            for(int i=candidates.Count-1;i>0;i--){int j=random.Next(i+1);var p=candidates[i];candidates[i]=candidates[j];candidates[j]=p;}
            if(count>candidates.Count)throw new ArgumentOutOfRangeException(nameof(count));
            var result=new Vector2[count];
            // 0.20 pitch / 0.16 body diameter, with at most 0.012 jitter per axis.
            for(int i=0;i<count;i++)result[i]=candidates[i]+new Vector2((float)(random.NextDouble()-.5)*.012f,(float)(random.NextDouble()-.5)*.012f);
            return result;
        }
    }
}
