using UnityEngine;
namespace PickDoll {
 // The front sign becomes translucent from above so it cannot hide the playfield.
 public sealed class CabinetViewFade : MonoBehaviour {
  public OrbitView orbit; public Renderer[] panels; public TextMesh label;
  public void Apply(){if(!orbit)return;float a=1-.9f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(30,65,orbit.elevation));foreach(var r in panels){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);var color=r.sharedMaterial.color;color.a=a;b.SetColor("_BaseColor",color);r.SetPropertyBlock(b);}if(label){var c=label.color;c.a=a;label.color=c;}}
  void LateUpdate()=>Apply();
 }
}
