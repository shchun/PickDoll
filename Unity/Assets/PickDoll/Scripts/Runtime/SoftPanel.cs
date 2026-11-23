using UnityEngine;
using UnityEngine.UI;
namespace PickDoll
{
    // Code-native rounded UI: no texture downloads or paid assets.
    public sealed class SoftPanel : MaskableGraphic
    {
        public float radius=24;
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();var r=rectTransform.rect;float corner=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
            vh.AddVert(r.center,color,Vector2.zero);
            var centers=new[]{new Vector2(r.xMax-corner,r.yMax-corner),new Vector2(r.xMin+corner,r.yMax-corner),new Vector2(r.xMin+corner,r.yMin+corner),new Vector2(r.xMax-corner,r.yMin+corner)};
            for(int c=0;c<4;c++)for(int i=0;i<=10;i++) {
                float angle=(c*90+i*9)*Mathf.Deg2Rad;
                vh.AddVert(centers[c]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*corner,color,Vector2.zero);
            }
            for(int i=1;i<=44;i++)vh.AddTriangle(0,i,i==44?1:i+1);
        }
    }
}
