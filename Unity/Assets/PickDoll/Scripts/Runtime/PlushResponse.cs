using UnityEngine;
namespace PickDoll {
    public sealed class PlushResponse : MonoBehaviour {
        public Transform visual;
        private Vector3 original;
        private float compression,target;
        private void Awake(){original=visual.localScale;}
        private void OnCollisionStay(Collision collision){target=Mathf.Max(target,Mathf.Clamp(collision.impulse.magnitude/.12f,0,.035f));}
        private void LateUpdate(){compression=Mathf.MoveTowards(compression,target,Time.deltaTime*.3f);target=Mathf.MoveTowards(target,0,Time.deltaTime*.16f);visual.localScale=Vector3.Scale(original,new Vector3(1+compression*.5f,1-compression,1+compression*.5f));}
    }
}
