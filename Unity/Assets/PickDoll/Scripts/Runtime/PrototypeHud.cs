using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PickDoll
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        public MachineController machine;
        public OrbitView orbit;
        private RectTransform safe;
        private Text mode;
        private Text position;
        private Font font;
        private ClawCycle pickup;
        private Button grab;
        private Rect lastSafe;
        private int width, height;
        private readonly Color ink = new Color(0.98f, 0.93f, 0.97f);
        private readonly Color pink = new Color(0.75f, 0.29f, 0.48f);
        private void Awake()
        {
            Application.targetFrameRate = 60;
            pickup = machine.GetComponent<ClawCycle>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var events = new GameObject("Input Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var canvas = new GameObject("Prototype HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600); scaler.matchWidthOrHeight = 0.5f;
            safe = Rect("Safe Area", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var drag = Rect("Drag to orbit", safe, Vector2.zero, Vector2.one, new Vector2(0, pickup ? 455 : 335), new Vector2(0, -180));
            drag.gameObject.AddComponent<Image>().color = Color.clear;
            drag.gameObject.AddComponent<OrbitDragArea>().orbit = orbit;
            Label("PICKDOLL", safe, new Vector2(0,1), new Vector2(0,1), new Vector2(300,-85), new Vector2(520,78), 48, TextAnchor.MiddleLeft);
            Label(pickup && pickup.smoothMotion ? "ANDROID / WEEK 04" : pickup && pickup.PassiveSuspension ? "ANDROID / WEEK 03" : pickup ? "ANDROID / WEEK 02" : "ANDROID / WEEK 01", safe, new Vector2(0,1), new Vector2(0,1), new Vector2(300,-133), new Vector2(520,44), 20, TextAnchor.MiddleLeft);
            Button("VIEW", safe, new Vector2(1,1), new Vector2(-115,-85), new Vector2(150,68), () => orbit.NextPreset());
            Label("DRAG TO LOOK AROUND", safe, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-190), new Vector2(650,40), 20);
            var panel = Rect("Controls", safe, Vector2.zero, new Vector2(1,0), new Vector2(20,20), new Vector2(-20,pickup ? 450 : 330));
            panel.gameObject.AddComponent<Image>().color = new Color(.16f,.11f,.18f,.96f);
            position = Label("", panel, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-35), new Vector2(760,38), 19);
            var up = Button("BACK", panel, new Vector2(.25f,.5f), new Vector2(0,58), new Vector2(115,66)); Hold(up,Vector2.up);
            var down = Button("FRONT", panel, new Vector2(.25f,.5f), new Vector2(0,-84), new Vector2(115,66)); Hold(down,Vector2.down);
            Hold(Button("LEFT",panel,new Vector2(.25f,.5f),new Vector2(-122,-13),new Vector2(115,66)),Vector2.left);
            Hold(Button("RIGHT",panel,new Vector2(.25f,.5f),new Vector2(122,-13),new Vector2(115,66)),Vector2.right);
            var fine = Button("PRECISION: OFF",panel,new Vector2(.75f,.5f),new Vector2(0,58),new Vector2(290,66),()=>machine.ToggleFine()); mode=fine.GetComponentInChildren<Text>();
            if (!pickup || !pickup.PassiveSuspension) {
            Hold(Button("TURN L",panel,new Vector2(.75f,.5f),new Vector2(-76,-13),new Vector2(138,66)),Vector2.zero,-1);
            Hold(Button("TURN R",panel,new Vector2(.75f,.5f),new Vector2(76,-13),new Vector2(138,66)),Vector2.zero,1);
            } else Label("PASSIVE SWING",panel,new Vector2(.75f,.5f),new Vector2(.75f,.5f),new Vector2(0,-13),new Vector2(290,50),20);
            Button("RESET",panel,new Vector2(.75f,.5f),new Vector2(0,-84),new Vector2(290,66),()=>machine.ResetPosition());
            if (pickup) grab = Button("GRAB",panel,new Vector2(.5f,0),new Vector2(0,55),new Vector2(760,56),()=>pickup.Press()).GetComponent<Button>();
            Label(pickup ? "GRAB / CARRY / OPEN — PHYSICS PROTOTYPE" : "MOVEMENT PROTOTYPE  /  PICKUP COMES IN WEEK 02",panel,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,pickup ? 12 : 25),new Vector2(790,pickup ? 20 : 30),16);
            ApplySafeArea();
        }
        private void Update()
        {
            if (lastSafe != Screen.safeArea || width != Screen.width || height != Screen.height) { machine.ReleaseControls(); ApplySafeArea(); }
            mode.text = machine.Fine ? "PRECISION: ON" : "PRECISION: OFF";
            var aim = machine.Aim;
            position.text = $"MACHINE AXES  /  X {aim.x:+0.000;-0.000;0.000}   Z {aim.y:+0.000;-0.000;0.000}" + (machine.AtLimit ? "  /  LIMIT" : "");
            if (pickup) { position.text = pickup.Status; grab.interactable = !pickup.Busy; grab.GetComponentInChildren<Text>().text = pickup.Phase == ClawCycle.Stage.Carry ? "OPEN" : pickup.Busy ? "WORKING..." : "GRAB"; }
        }
        private void ApplySafeArea()
        {
            lastSafe = Screen.safeArea; width = Screen.width; height = Screen.height;
            safe.anchorMin = new Vector2(lastSafe.xMin / width, lastSafe.yMin / height);
            safe.anchorMax = new Vector2(lastSafe.xMax / width, lastSafe.yMax / height);
        }
        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=min; rect.anchorMax=max;
            rect.offsetMin=low; rect.offsetMax=high;
            return rect;
        }
        private Text Label(string text,Transform parent,Vector2 min,Vector2 max,Vector2 pos,Vector2 size,int fontSize,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var rect=Rect(text,parent,min,max,Vector2.zero,Vector2.zero); rect.sizeDelta=size; rect.anchoredPosition=pos;
            var label=rect.gameObject.AddComponent<Text>(); label.font=font; label.text=text; label.fontSize=fontSize;
            label.alignment=align; label.color=ink; label.raycastTarget=false; return label;
        }
        private GameObject Button(string text,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction clicked=null)
        {
            var rect=Rect(text,parent,anchor,anchor,Vector2.zero,Vector2.zero); rect.sizeDelta=size; rect.anchoredPosition=pos;
            rect.gameObject.AddComponent<Image>().color=pink;
            var button=rect.gameObject.AddComponent<Button>();
            if(clicked!=null) button.onClick.AddListener(clicked);
            Label(text,rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(8,8),20);
            return rect.gameObject;
        }
        private void Hold(GameObject button,Vector2 direction,float rotate=0)
        {
            var hold=button.AddComponent<HoldControl>(); hold.machine=machine; hold.direction=direction; hold.rotation=rotate;
        }
    }
}
