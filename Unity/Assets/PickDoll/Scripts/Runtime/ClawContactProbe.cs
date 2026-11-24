using System.Collections.Generic;
using UnityEngine;
namespace PickDoll
{
    // Collision impulses are from the previous physics step, never inferred from visual overlap.
    public sealed class ClawContactProbe : MonoBehaviour
    {
        private struct Sample {public float time,force;public bool prize;}
        private readonly Dictionary<Collider,Sample> contacts=new Dictionary<Collider,Sample>();
        private void OnCollisionEnter(Collision hit)=>Record(hit);
        private void OnCollisionStay(Collision hit)=>Record(hit);
        private void OnCollisionExit(Collision hit)=>contacts.Remove(hit.collider);
        private void Record(Collision hit) {
            bool vertical=false;for(int i=0;i<hit.contactCount;i++)if(Mathf.Abs(hit.GetContact(i).normal.y)>.45f){vertical=true;break;}
            contacts[hit.collider]=new Sample {time=Time.fixedTime,force=vertical?Mathf.Abs(hit.impulse.y)/Mathf.Max(.001f,Time.fixedDeltaTime):0,prize=hit.collider.GetComponentInParent<PrizeIdentity>()!=null};
        }
        public void Read(out float prize,out float structure) {
            prize=structure=0;
            foreach(var entry in contacts)if(entry.Key&&Time.fixedTime-entry.Value.time<=Time.fixedDeltaTime*2.1f) {
                if(entry.Value.prize)prize+=entry.Value.force;else structure+=entry.Value.force;
            }
        }
        public void Clear()=>contacts.Clear();
    }
}
