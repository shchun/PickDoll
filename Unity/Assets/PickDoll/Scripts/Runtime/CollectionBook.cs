using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PickDoll {
 [DefaultExecutionOrder(200)]
 public sealed class CollectionBook : MonoBehaviour {
  public Material silhouetteMaterial;public PrizeReceiver receiver;public Rigidbody[] models;public MachineController machine;public ComboTracker combo;
  public bool IsOpen=>modal&&modal.gameObject.activeSelf;
  public int SelectedIndex {get;private set;}
  private RectTransform modal,blocker;private Transform safe,stage,preview;private Camera previewCamera;private RenderTexture texture;
  private Text caption,comboText;private Text[] entries;private Font font;private Material silhouette;private float previousTimeScale=1;private bool machineWasEnabled;
  private void Awake(){
   font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");safe=GameObject.Find("Safe Area").transform;
   var open=Panel("Collection button",safe,new Vector2(.5f,1),new Vector2(0,-129),new Vector2(165,44),new Color(.32f,.14f,.26f));Button(open,"COLLECTION",()=>SetOpen(!IsOpen),17);
   var meter=Panel("Combo meter",safe,new Vector2(.5f,0),new Vector2(0,510),new Vector2(290,42),new Color(.16f,.08f,.19f,.8f));meter.GetComponent<SoftPanel>().raycastTarget=false;comboText=Label(meter,"",20);
   blocker=Panel("Collection blocker",safe,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(.03f,.02f,.04f,.5f));blocker.anchorMin=Vector2.zero;blocker.anchorMax=Vector2.one;blocker.offsetMin=blocker.offsetMax=Vector2.zero;
   modal=Panel("Collection modal",safe,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(.12f,.07f,.16f,.98f));modal.anchorMin=new Vector2(.035f,.06f);modal.anchorMax=new Vector2(.965f,.94f);modal.offsetMin=modal.offsetMax=Vector2.zero;
   var close=Panel("Close collection",modal,new Vector2(1,1),new Vector2(-74,-40),new Vector2(125,48),new Color(.63f,.23f,.41f));Button(close,"CLOSE",()=>SetOpen(false),20);
   var heading=Panel("Book heading",modal,new Vector2(.5f,1),new Vector2(-65,-42),new Vector2(410,50),Color.clear);Label(heading,"MY PLUSH COLLECTION",25);
   var view=Panel("Model preview",modal,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,Color.clear);view.anchorMin=new Vector2(.08f,.49f);view.anchorMax=new Vector2(.92f,.89f);view.offsetMin=view.offsetMax=Vector2.zero;
   view.GetComponent<SoftPanel>().raycastTarget=false;var imageRect=new GameObject("Preview image",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();imageRect.SetParent(view,false);imageRect.anchorMin=Vector2.zero;imageRect.anchorMax=Vector2.one;imageRect.offsetMin=imageRect.offsetMax=Vector2.zero;var image=imageRect.GetComponent<RawImage>();image.color=Color.white;var fit=imageRect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;var drag=imageRect.gameObject.AddComponent<BookModelDrag>();drag.book=this;
   stage=new GameObject("Collection preview stage").transform;stage.position=new Vector3(1000,1000,1000);
   var cameraObject=new GameObject("Collection preview camera",typeof(Camera));cameraObject.transform.SetParent(stage,false);previewCamera=cameraObject.GetComponent<Camera>();previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.19f,.12f,.22f);previewCamera.cullingMask=1<<30;previewCamera.orthographic=true;previewCamera.orthographicSize=.21f;
   texture=new RenderTexture(384,384,16);texture.Create();previewCamera.targetTexture=texture;image.texture=texture;
   var lamp=new GameObject("Preview lamp",typeof(Light));lamp.transform.SetParent(stage,false);lamp.transform.localPosition=new Vector3(-.3f,.5f,-.5f);lamp.transform.LookAt(stage.position+Vector3.up*.08f);var light=lamp.GetComponent<Light>();light.type=LightType.Spot;light.spotAngle=70;light.range=3;light.intensity=1.3f;light.cullingMask=1<<30;
   silhouette=new Material(silhouetteMaterial);silhouette.color=new Color(.035f,.022f,.045f);
   var title=Panel("Selected plush name",modal,new Vector2(.5f,.46f),Vector2.zero,new Vector2(650,44),Color.clear);caption=Label(title,"",25);
   entries=new Text[models.Length];for(int i=0;i<models.Length;i++){int index=i;var row=Panel("Species "+i,modal,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(.31f,.16f,.29f));row.anchorMin=new Vector2(.07f,.06f+(models.Length-1-i)*.049f);row.anchorMax=new Vector2(.93f,.102f+(models.Length-1-i)*.049f);row.offsetMin=row.offsetMax=Vector2.zero;entries[i]=Button(row,"",()=>Select(index),20);}
   modal.gameObject.SetActive(false);blocker.gameObject.SetActive(false);previewCamera.gameObject.SetActive(false);
  }
  private Text Button(RectTransform target,string value,UnityEngine.Events.UnityAction action,int size){var b=target.gameObject.AddComponent<Button>();b.targetGraphic=target.GetComponent<SoftPanel>();b.onClick.AddListener(action);return Label(target,value,size);}
  private RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color){var r=new GameObject(name,typeof(RectTransform),typeof(SoftPanel)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;r.sizeDelta=size;r.GetComponent<SoftPanel>().color=color;r.GetComponent<SoftPanel>().radius=16;return r;}
  private Text Label(Transform parent,string value,int size){var r=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8,0);r.offsetMax=new Vector2(-8,0);var t=r.GetComponent<Text>();t.font=font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(1,.93f,.88f);t.text=value;t.raycastTarget=false;return t;}
  public void SetOpen(bool value){if(IsOpen==value)return;machine.ReleaseControls();FindAnyObjectByType<AnalogJoystick>().CancelTouch();FindAnyObjectByType<OrbitDragArea>().CancelTouches();if(value){machineWasEnabled=machine.enabled;machine.enabled=false;previousTimeScale=Time.timeScale;Time.timeScale=0;}else {Time.timeScale=previousTimeScale;machine.enabled=machineWasEnabled;}blocker.gameObject.SetActive(value);modal.gameObject.SetActive(value);previewCamera.gameObject.SetActive(value);if(value){blocker.SetAsLastSibling();modal.SetAsLastSibling();Select(SelectedIndex);}}
  public void Select(int index){SelectedIndex=Mathf.Clamp(index,0,models.Length-1);if(preview)Destroy(preview.gameObject);preview=new GameObject("Preview model").transform;preview.SetParent(stage,false);var body=models[SelectedIndex];var identity=body.GetComponent<PrizeIdentity>();bool unlocked=receiver.Store!=null&&receiver.Store.CountFor(identity.SpeciesId)>0;
   foreach(var source in body.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled)) {var filter=source.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;var part=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));part.layer=30;part.transform.SetParent(preview,false);part.transform.localPosition=body.transform.InverseTransformPoint(source.transform.position);part.transform.localRotation=Quaternion.Inverse(body.rotation)*source.transform.rotation;part.transform.localScale=source.transform.lossyScale;part.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;part.GetComponent<Renderer>().sharedMaterials=unlocked?source.sharedMaterials:Enumerable.Repeat(silhouette,source.sharedMaterials.Length).ToArray();}
   preview.localRotation=Quaternion.Euler(0,-20,0);previewCamera.transform.localPosition=new Vector3(0,.08f,-1);previewCamera.transform.LookAt(stage.position+Vector3.up*.08f);caption.text=unlocked?identity.displayName+"  /  "+receiver.Store.CountFor(identity.SpeciesId):"UNDISCOVERED  /  DRAG TO ROTATE";Refresh();}
  public void Rotate(float delta){if(preview)preview.Rotate(Vector3.up,-delta*.4f,Space.World);}
  public void Refresh(){if(entries==null)return;for(int i=0;i<models.Length;i++){var id=models[i].GetComponent<PrizeIdentity>();int count=receiver.Store?.CountFor(id.SpeciesId)??0;entries[i].text=(count>0?id.displayName:"???")+"     "+count;}}
  private void Update(){if(comboText){comboText.transform.parent.gameObject.SetActive(combo.Remaining>0);comboText.text=combo.Remaining>0?(combo.Chain.Count>1?combo.Chain.Count+" COMBO":"NEXT PLUSH")+"  "+combo.Remaining.ToString("0.0")+"s":"";}if(IsOpen)Refresh();}
  private void OnDestroy(){if(IsOpen)Time.timeScale=previousTimeScale;if(stage)Destroy(stage.gameObject);if(texture){texture.Release();Destroy(texture);}if(silhouette)Destroy(silhouette);}
 }
 public sealed class BookModelDrag : MonoBehaviour,IDragHandler {public CollectionBook book;public void OnDrag(PointerEventData e)=>book.Rotate(e.delta.x);}
}
