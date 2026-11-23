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
        public void Restock() {
            var slots=Layout(Environment.TickCount,prizes.Length);
            for(int i=0;i<prizes.Length;i++) {
                var body=prizes[i];body.gameObject.SetActive(true);body.isKinematic=false;
                body.position=new Vector3(slots[i].x,spawnY,slots[i].y);body.rotation=Quaternion.Euler(0,UnityEngine.Random.Range(-35f,35f),0);
                body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
                body.GetComponent<PrizeIdentity>().Rearm();body.WakeUp();
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
