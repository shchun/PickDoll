using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace PickDoll
{
    public sealed class WeekSixHud : MonoBehaviour
    {
        public MachineController machine;
        public OrbitView orbit;
        private ClawCycle cycle;
        private Font font;
        private RectTransform safe;
        private Rect previousSafe;
        private Vector2 previousSize;
        private Text count,status,grabText,zoomText;
        private Button grab;
        private AnalogJoystick joystick;
        private OrbitDragArea drag;
        private readonly Color cream=new Color(1,.94f,.91f);
        private readonly Color plum=new Color(.22f,.10f,.20f);
        private void Awake() {
            Application.targetFrameRate=60;cycle=machine.GetComponent<ClawCycle>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var events=new GameObject("Input Events",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var canvas=new GameObject("PickDoll Touch HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(900,1600);scaler.matchWidthOrHeight=.5f;
            safe=Rect("Safe Area",canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            // The 3D camera only clears its viewport. Opaque UI backdrops clear changing
            // text outside that viewport instead of retaining pixels from earlier frames.
            var header=Rect("Header backdrop",safe,new Vector2(0,1),Vector2.one,new Vector2(0,-205),Vector2.zero);
            var footer=Rect("Controls backdrop",safe,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,440));
            foreach(var backdrop in new[]{header,footer}){var image=backdrop.gameObject.AddComponent<Image>();image.color=new Color(.08f,.055f,.105f);image.raycastTarget=false;}
            var cameraArea=Rect("Camera gestures",safe,Vector2.zero,Vector2.one,new Vector2(0,430),new Vector2(0,-205));
            cameraArea.gameObject.AddComponent<Image>().color=Color.clear;drag=cameraArea.gameObject.AddComponent<OrbitDragArea>();drag.orbit=orbit;
            Label("PickDoll",safe,new Vector2(0,1),new Vector2(205,-65),new Vector2(330,74),48,cream,TextAnchor.MiddleLeft);
            Label("SWEET LITTLE WINS",safe,new Vector2(0,1),new Vector2(205,-112),new Vector2(330,32),18,new Color(1,.64f,.76f),TextAnchor.MiddleLeft);
            var score=Panel("Collection counter",safe,new Vector2(1,1),new Vector2(-132,-130),new Vector2(190,100),new Color(.23f,.12f,.22f),24);
            Label("COLLECTED",score,new Vector2(.5f,.5f),new Vector2(0,28),new Vector2(170,25),18,cream);
            count=Label("0",score,new Vector2(.5f,.5f),new Vector2(0,-13),new Vector2(170,64),56,Color.white);
            Button("VIEW",safe,new Vector2(1,1),new Vector2(-132,-40),new Vector2(190,48),()=>orbit.NextPreset(),new Color(.43f,.23f,.37f),20);
            zoomText=Label("DRAG TO LOOK  /  PINCH TO ZOOM",safe,new Vector2(.5f,1),new Vector2(0,-190),new Vector2(760,30),18,cream);
            var panel=Rect("Touch controls",safe,Vector2.zero,new Vector2(1,0),new Vector2(20,20),new Vector2(-20,420));
            var surface=panel.gameObject.AddComponent<SoftPanel>();surface.radius=36;surface.color=new Color(.98f,.89f,.88f);
            status=Label("READY FOR A LITTLE WIN?",panel,new Vector2(.5f,1),new Vector2(0,-38),new Vector2(780,34),23,plum);
            var baseRing=Panel("Drag joystick",panel,new Vector2(.25f,.5f),new Vector2(0,-4),new Vector2(254,254),new Color(.87f,.68f,.75f),127);
            Panel("Joystick well",baseRing,new Vector2(.5f,.5f),Vector2.zero,new Vector2(228,228),new Color(.94f,.79f,.82f),114,false);
            foreach(var item in new[]{new Vector3(0,99,0),new Vector3(0,-99,0),new Vector3(-99,0,0),new Vector3(99,0,0)})Panel("Direction dot",baseRing,new Vector2(.5f,.5f),item,new Vector2(7,7),new Color(.64f,.37f,.49f),4,false);
            var knob=Panel("Joystick knob",baseRing,new Vector2(.5f,.5f),Vector2.zero,new Vector2(86,86),new Color(.72f,.25f,.45f),43,false);
            Panel("Paw pad",knob,new Vector2(.5f,.5f),new Vector2(0,-10),new Vector2(33,27),cream,14,false);
            for(int i=0;i<3;i++)Panel("Paw toe",knob,new Vector2(.5f,.5f),new Vector2((i-1)*19,14+(i==1?5:0)),new Vector2(14,17),cream,8,false);
            joystick=baseRing.gameObject.AddComponent<AnalogJoystick>();joystick.machine=machine;joystick.knob=knob;
            Label("DRAG TO AIM",panel,new Vector2(.25f,0),new Vector2(0,40),new Vector2(300,28),19,new Color(.46f,.26f,.36f));
            grab=Button("GRAB!",panel,new Vector2(.75f,.5f),new Vector2(0,18),new Vector2(330,178),()=>cycle.Press(),new Color(.75f,.20f,.42f),38);
            grabText=grab.GetComponentInChildren<Text>();
            Button("NEW ROUND",panel,new Vector2(.75f,0),new Vector2(0,65),new Vector2(250,56),()=>{joystick.CancelTouch();machine.ResetPosition();},new Color(.43f,.24f,.36f),19);
            ApplySafeArea();
        }
        private void Update() {
            if(previousSafe!=Screen.safeArea||previousSize!=new Vector2(Screen.width,Screen.height)){joystick.CancelTouch();drag.CancelTouches();machine.ReleaseControls();ApplySafeArea();}
            count.text=cycle.receiver.Count.ToString();
            grab.interactable=cycle.CanPress;grabText.text=cycle.Busy?"ON MY WAY...":cycle.CanPress?"GRAB!":"MOVE TO AIM";
            grabText.fontSize=cycle.Busy||!cycle.CanPress?26:38;
            status.text=cycle.Phase==ClawCycle.Stage.Ready?"READY FOR A LITTLE WIN?":cycle.Status;
            zoomText.text=orbit.Zoom>1.01f?$"ZOOM {orbit.Zoom:0.0}x  /  VIEW TO RESET":"DRAG TO LOOK  /  PINCH TO ZOOM";
        }
        private void ApplySafeArea(){previousSafe=Screen.safeArea;previousSize=new Vector2(Screen.width,Screen.height);safe.anchorMin=new Vector2(previousSafe.xMin/Screen.width,previousSafe.yMin/Screen.height);safe.anchorMax=new Vector2(previousSafe.xMax/Screen.width,previousSafe.yMax/Screen.height);}
        private RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 low,Vector2 high){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=low;r.offsetMax=high;return r;}
        private RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color,float radius,bool touch=true){var r=Rect(name,parent,anchor,anchor,Vector2.zero,Vector2.zero);r.sizeDelta=size;r.anchoredPosition=pos;var g=r.gameObject.AddComponent<SoftPanel>();g.color=color;g.radius=radius;g.raycastTarget=touch;return r;}
        private Text Label(string value,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor align=TextAnchor.MiddleCenter){var r=Rect(value,parent,anchor,anchor,Vector2.zero,Vector2.zero);r.sizeDelta=size;r.anchoredPosition=pos;var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=fontSize;t.fontStyle=FontStyle.Bold;t.color=color;t.alignment=align;t.raycastTarget=false;return t;}
        private Button Button(string text,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,Color color,int fontSize){var r=Panel(text,parent,anchor,pos,size,color,Mathf.Min(44,size.y*.5f));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<SoftPanel>();b.onClick.AddListener(action);Label(text,r,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(16,8),fontSize,cream);return b;}
    }
}
