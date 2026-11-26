using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
namespace PickDoll.Editor {
 public static class WeekTwelvePreview {
  public static void Run(){WeekTwelveBuilder.Generate();WeekTwelveDiagnostics.ValidateChute();WeekTwelveDiagnostics.ValidateStability();EditorApplication.delayCall+=Capture;}
  static void Capture(){try{var camera=Camera.main;var orbit=camera.GetComponent<OrbitView>();var rp=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=rp;QualitySettings.renderPipeline=rp;Directory.CreateDirectory("Builds/Week12/Previews");foreach(var pose in new[]{new Vector3(12,15,1),new Vector3(12,55,2),new Vector3(65,25,1),new Vector3(0,25,2)}){camera.aspect=3f/4;orbit.yaw=pose.x;orbit.elevation=pose.y;orbit.ZoomByRatio(pose.z/orbit.Zoom);orbit.Apply();camera.rect=new Rect(0,0,1,1);camera.aspect=3f/4;camera.ResetProjectionMatrix();var matrix=camera.projectionMatrix;matrix[1,2]=2*orbit.zoomScreenOffset*Mathf.InverseLerp(1,2,orbit.Zoom);camera.projectionMatrix=matrix;Object.FindAnyObjectByType<CabinetViewFade>().Apply();var rt=new RenderTexture(900,1200,24);rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(900,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,1200),0,0);image.Apply();File.WriteAllBytes($"Builds/Week12/Previews/view-{pose.x}-{pose.y}-{pose.z}.png",image.EncodeToPNG());RenderTexture.active=old;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}Debug.Log("PICKDOLL_WEEK12_PREVIEW_READY");EditorApplication.Exit(0);}catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
