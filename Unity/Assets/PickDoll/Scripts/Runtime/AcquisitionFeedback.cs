using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PickDoll
{
    [DefaultExecutionOrder(100)]
    public sealed class AcquisitionFeedback : MonoBehaviour
    {
        public PrizeReceiver receiver;
        public Renderer[] ledStrips;
        public bool celebration;
        public bool arcadeStyle;
        public bool HasPending=>names.Count>0||Visible;
        public int RestockPresentations {get;private set;}
        private ArcadeSound arcade;
        private RectTransform safeArea;
        private OrbitView gameView;
        private bool restockCard;
        private RectTransform[] sparks;
        public bool SoundEnabled {get;private set;}
        public bool HapticsEnabled {get;private set;}
        public int AcceptedCount {get;private set;}
        public int PresentedCount {get;private set;}
        public bool Visible=>toast&&toast.gameObject.activeSelf;
        public string CurrentName {get;private set;}
        private readonly HashSet<string> seen=new HashSet<string>();
        private readonly Queue<string> names=new Queue<string>();
        private readonly Queue<string> species=new Queue<string>();
        private RectTransform toast,counter,face;
        private CanvasGroup group;
        private Text title,soundLabel,hapticsLabel,banner;
        private readonly Queue<int> comboCounts=new Queue<int>();
        private ComboTracker combo;
        private AudioSource speaker;
        private AudioClip chime;
        private Material[] lamps;
        private Color[] emissions;
        private float age=3,pulse;
        private bool paused;
        private string key="PickDoll.feedback";
        private Font font;
        private void Awake() {
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--pickdoll-week09-")||arg.StartsWith("--pickdoll-week10-")||arg.StartsWith("--pickdoll-week11-"))key+=".test."+Guid.NewGuid().ToString("N");
            LoadPreferences();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var safe=GameObject.Find("Safe Area").transform;safeArea=(RectTransform)safe;gameView=FindAnyObjectByType<OrbitView>();arcade=GetComponent<ArcadeSound>();
            counter=GameObject.Find("Collection counter").GetComponent<RectTransform>();
            toast=Panel("Acquisition card",safe,celebration?new Vector2(.5f,.56f):new Vector2(.5f,1),celebration?Vector2.zero:new Vector2(0,-230),celebration?new Vector2(590,280):new Vector2(550,145),new Color(.99f,.92f,.84f));
            group=toast.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            combo=GetComponent<ComboTracker>();
            banner=Label(celebration?"YOU GOT IT!  +1":"GOT IT!  +1",toast,new Vector2(60,celebration?72:37),new Vector2(360,60),celebration?42:32,new Color(.58f,.12f,.32f));
            title=Label("",toast,new Vector2(60,celebration?-65:-24),new Vector2(360,42),25,new Color(.24f,.12f,.21f));
            face=Panel("Prize face",toast,new Vector2(.5f,.5f),new Vector2(-192,0),new Vector2(100,100),Color.clear);
            SettingsButton("Sound toggle",safe,new Vector2(105,-129),()=>SetSound(!SoundEnabled),out soundLabel);
            SettingsButton("Haptics toggle",safe,new Vector2(-105,-129),()=>SetHaptics(!HapticsEnabled),out hapticsLabel,true);
            if(celebration){sparks=new RectTransform[14];for(int i=0;i<sparks.Length;i++){var t=Label(i%2==0?"★":"✦",toast,Vector2.zero,new Vector2(45,45),32,i%2==0?new Color(1,.65f,.12f):new Color(.95f,.28f,.52f));sparks[i]=t.rectTransform;}}
            if(arcadeStyle){toast.sizeDelta=new Vector2(Mathf.Min(740,safeArea.rect.width-40),350);toast.GetComponent<SoftPanel>().color=new Color(.99f,.86f,.91f,.96f);toast.anchorMin=toast.anchorMax=new Vector2(.5f,0);banner.rectTransform.anchoredPosition=new Vector2(0,105);banner.rectTransform.sizeDelta=new Vector2(toast.sizeDelta.x-30,88);banner.fontSize=68;banner.resizeTextForBestFit=true;banner.resizeTextMinSize=36;banner.resizeTextMaxSize=68;title.rectTransform.anchoredPosition=new Vector2(0,-120);title.rectTransform.sizeDelta=new Vector2(toast.sizeDelta.x-30,48);title.fontSize=28;face.anchoredPosition=new Vector2(0,-5);foreach(var spark in sparks)spark.SetAsFirstSibling();}
            RefreshLabels();toast.gameObject.SetActive(false);
            speaker=gameObject.AddComponent<AudioSource>();speaker.playOnAwake=false;speaker.spatialBlend=0;speaker.volume=.45f;
            var notes=celebration?new[]{523.25f,659.25f,783.99f,659.25f,783.99f,1046.5f}:new[]{659.25f,830.61f,987.77f};const int rate=44100;var samples=new float[(int)(rate*(celebration?1.1f:.36f))];
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;float beat=celebration?.16f:.12f;int note=Mathf.Min(notes.Length-1,(int)(t/beat));float local=t-note*beat;float envelope=Mathf.Min(local/.008f,1)*Mathf.Exp(-local*(celebration&&note==notes.Length-1?9:24));samples[i]=.28f*envelope*(Mathf.Sin(2*Mathf.PI*notes[note]*t)+(celebration?.24f*Mathf.Sin(2*Mathf.PI*notes[note]*1.5f*t):0));}
            chime=AudioClip.Create("PickDoll original success chime",samples.Length,1,rate,false);chime.SetData(samples,0);
            lamps=new Material[ledStrips.Length];emissions=new Color[lamps.Length];
            for(int i=0;i<lamps.Length;i++){lamps[i]=ledStrips[i].material;emissions[i]=lamps[i].GetColor("_EmissionColor");}
        }
        private void OnEnable(){receiver.Acquired+=Receive;}
        private void OnDisable(){receiver.Acquired-=Receive;ResetVisuals();}
        public void LoadPreferences(){SoundEnabled=PlayerPrefs.GetInt(key+".sound",1)!=0;HapticsEnabled=PlayerPrefs.GetInt(key+".haptics",1)!=0;RefreshLabels();}
        public void SetSound(bool enabled){SoundEnabled=enabled;PlayerPrefs.SetInt(key+".sound",enabled?1:0);PlayerPrefs.Save();if(!enabled&&speaker)speaker.Stop();if(arcade)arcade.SetMuted(!enabled);RefreshLabels();}
        public void SetHaptics(bool enabled){HapticsEnabled=enabled;PlayerPrefs.SetInt(key+".haptics",enabled?1:0);PlayerPrefs.Save();RefreshLabels();}
        private void RefreshLabels(){if(soundLabel)soundLabel.text=SoundEnabled?"SOUND ON":"SOUND OFF";if(hapticsLabel)hapticsLabel.text=HapticsEnabled?"HAPTIC ON":"HAPTIC OFF";}
        public void Receive(PrizeIdentity prize){if(!prize.Delivered||!seen.Add(prize.Ticket))return;AcceptedCount++;names.Enqueue(prize.displayName);species.Enqueue(prize.SpeciesId);comboCounts.Enqueue(combo?combo.Register(prize):1);}
        private void Update(){
            if(paused)return;
            if(arcadeStyle){toast.anchoredPosition=new Vector2(0,(safeArea.rect.height+(gameView?gameView.controlsHeight:440))/2);toast.sizeDelta=new Vector2(Mathf.Min(740,safeArea.rect.width-40),350);}
            float duration=arcadeStyle?(restockCard?1.4f:1.1f):celebration?1.85f:1.15f;
            if(age>=duration&&names.Count>0){restockCard=false;CurrentName=names.Dequeue();title.text=CurrentName;int chain=comboCounts.Dequeue();banner.text=chain>1?$"{chain} COMBO! +1":celebration?"YOU GOT IT! +1":"GOT IT! +1";DrawFace(species.Dequeue());age=0;pulse=.65f;PresentedCount++;toast.gameObject.SetActive(true);if(arcadeStyle&&arcade)arcade.PlayAcquisition(chain);else if(SoundEnabled)speaker.PlayOneShot(chime);if(HapticsEnabled)Vibrate();}
            age+=Time.unscaledDeltaTime;pulse=Mathf.Max(0,pulse-Time.unscaledDeltaTime);
            float scale=1+.16f*Mathf.Sin(Mathf.Clamp01(pulse/.65f)*Mathf.PI);counter.localScale=Vector3.one*scale;
            for(int i=0;i<lamps.Length;i++)lamps[i].SetColor("_EmissionColor",emissions[i]*(1+.55f*pulse/.65f));
            if(Visible){group.alpha=Mathf.Min(Mathf.Clamp01(age/.12f),Mathf.Clamp01((duration-age)/.18f));toast.localScale=Vector3.one*Mathf.Lerp(.84f,1,Mathf.Clamp01(age/.18f));if(celebration){face.localRotation=Quaternion.Euler(0,0,Mathf.Sin(age*12)*8*Mathf.Exp(-age*2));for(int i=0;i<sparks.Length;i++){float a=i*Mathf.PI*2/sparks.Length;float radius=Mathf.Lerp(70,320,Mathf.Clamp01(age/.7f));sparks[i].anchoredPosition=new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.7f);sparks[i].localScale=Vector3.one*Mathf.Clamp01((duration-age)/.5f);}}if(age>=duration)toast.gameObject.SetActive(false);}
        }
        public void ShowRestock(){restockCard=true;RestockPresentations++;CurrentName="New plushies";banner.text="NEW PLUSHIES!";title.text="7 fresh toys — ready to play";DrawMascot();age=0;pulse=.65f;toast.gameObject.SetActive(true);if(arcade)arcade.PlayRefill();}
        private void DrawFace(string id){
            if(celebration){DrawMascot();return;}
            foreach(Transform child in face)Destroy(child.gameObject);
            var color=id=="rabbit"?new Color(.90f,.40f,.60f):id=="penguin"?new Color(.30f,.42f,.64f):new Color(.32f,.75f,.63f);
            if(id!="penguin")foreach(float sign in new[]{-1f,1f})Panel("Ear",face,new Vector2(.5f,.5f),new Vector2(sign*29,35),new Vector2(id=="rabbit"?20:28,id=="rabbit"?60:28),color);
            Panel("Face",face,new Vector2(.5f,.5f),new Vector2(0,-2),new Vector2(86,72),color);
            foreach(float sign in new[]{-1f,1f})Panel("Eye",face,new Vector2(.5f,.5f),new Vector2(sign*17,2),new Vector2(8,9),new Color(.14f,.09f,.16f));
            Panel("Muzzle",face,new Vector2(.5f,.5f),new Vector2(0,-17),new Vector2(25,id=="penguin"?10:18),id=="penguin"?new Color(1,.63f,.22f):new Color(1,.89f,.72f));
        }
        private void DrawMascot(){
            foreach(Transform child in face)Destroy(child.gameObject);
            var ink=new Color(.10f,.07f,.13f);var ivory=new Color(.99f,.97f,.92f);var pink=new Color(.92f,.57f,.65f);
            foreach(float sign in new[]{-1f,1f}){
                var ear=Panel("Black upright ear",face,new Vector2(.5f,.5f),new Vector2(sign*26,45),new Vector2(22,76),ink);ear.localRotation=Quaternion.Euler(0,0,sign*-9);
                Panel("Inner ear",ear,new Vector2(.5f,.5f),Vector2.zero,new Vector2(10,53),pink);
            }
            Panel("White face",face,new Vector2(.5f,.5f),new Vector2(0,-2),new Vector2(105,88),ivory);
            foreach(float sign in new[]{-1f,1f}){
                Panel("Black eye patch",face,new Vector2(.5f,.5f),new Vector2(sign*31,4),new Vector2(30,48),ink);
                Label("^",face,new Vector2(sign*31,0),new Vector2(26,27),28,ivory);
                Panel("Blush",face,new Vector2(.5f,.5f),new Vector2(sign*35,-21),new Vector2(18,9),pink);
            }
            Panel("Nose",face,new Vector2(.5f,.5f),new Vector2(0,-14),new Vector2(10,7),pink);
            Label("w",face,new Vector2(0,-26),new Vector2(25,24),21,ink);
        }
        private void ResetVisuals(){if(toast)toast.gameObject.SetActive(false);if(counter)counter.localScale=Vector3.one;if(speaker)speaker.Stop();age=3;pulse=0;if(lamps!=null)for(int i=0;i<lamps.Length;i++)if(lamps[i])lamps[i].SetColor("_EmissionColor",emissions[i]);}
        private void OnApplicationPause(bool value){paused=value;if(value)ResetVisuals();}
        private void OnDestroy(){if(chime)Destroy(chime);if(lamps!=null)foreach(var lamp in lamps)if(lamp)Destroy(lamp);}
        private void Vibrate(){
#if UNITY_ANDROID && !UNITY_EDITOR
            try {using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))using(var vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator"))using(var effects=new AndroidJavaClass("android.os.VibrationEffect")){if(vibrator.Call<bool>("hasVibrator"))using(var effect=celebration?effects.CallStatic<AndroidJavaObject>("createWaveform",new long[]{0,90,65,130},new int[]{0,210,0,255},-1):effects.CallStatic<AndroidJavaObject>("createOneShot",35L,-1))vibrator.Call("vibrate",effect);}}
            catch(AndroidJavaException e){Debug.LogWarning("PickDoll haptics unavailable: "+e.Message);}
#endif
        }
        private RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=pos;var image=r.gameObject.AddComponent<SoftPanel>();image.color=color;image.radius=Mathf.Min(28,size.y*.5f);image.raycastTarget=false;return r;}
        private Text Label(string text,Transform parent,Vector2 pos,Vector2 size,int fontSize,Color color){var r=new GameObject(text,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;var label=r.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.fontStyle=FontStyle.Bold;label.alignment=TextAnchor.MiddleCenter;label.color=color;label.raycastTarget=false;return label;}
        private void SettingsButton(string name,Transform parent,Vector2 pos,UnityEngine.Events.UnityAction action,out Text text,bool right=false){var r=Panel(name,parent,new Vector2(right?1:0,1),pos,new Vector2(170,44),new Color(.25f,.12f,.22f,.9f));r.GetComponent<SoftPanel>().raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<SoftPanel>();b.navigation=new Navigation {mode=Navigation.Mode.None};b.onClick.AddListener(()=>{action();UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);});text=Label(name,r,Vector2.zero,new Vector2(164,40),17,new Color(1,.93f,.88f));}
    }
}
