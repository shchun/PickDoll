using UnityEngine;
namespace PickDoll {
    public sealed class CoilCableVisual : MonoBehaviour {
        public Transform top,bottom;public LineRenderer coil;
        private void LateUpdate(){var a=top.position+Vector3.down*.02f;var b=bottom.position+Vector3.up*.10f;var axis=b-a;var forward=axis.normalized;var side=Vector3.Cross(forward,Vector3.forward).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;var other=Vector3.Cross(forward,side);for(int i=0;i<65;i++){float t=i/64f;float angle=t*Mathf.PI*2*8;coil.SetPosition(i,Vector3.Lerp(a,b,t)+(side*Mathf.Cos(angle)+other*Mathf.Sin(angle))*.009f*Mathf.Sin(Mathf.PI*t));}}
    }
}
