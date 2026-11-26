using UnityEngine;
using UnityEngine.UI;
namespace PickDoll
{
    [DefaultExecutionOrder(150)]
    public sealed class ArcadeSound : MonoBehaviour
    {
        public AudioClip collect,combo,refill,ui,grab,clamp,release;
        private AudioSource celebration,actions;
        private AcquisitionFeedback feedback;
        private ClawCycle cycle;
        private ClawCycle.Stage previous;
        private bool paused;
        public int AcquisitionPlays {get;private set;}
        public int RefillPlays {get;private set;}
        private void Awake(){feedback=GetComponent<AcquisitionFeedback>();cycle=GetComponent<ClawCycle>();celebration=Source();actions=Source();previous=cycle.Phase;}
        private AudioSource Source(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.volume=.55f;return s;}
        private void Start(){foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))button.onClick.AddListener(()=>Play(actions,ui,.28f));}
        private bool Play(AudioSource source,AudioClip clip,float volume){if(paused||!feedback.SoundEnabled||!source||!clip)return false;source.PlayOneShot(clip,volume);return true;}
        public void PlayAcquisition(int chain){if(Play(celebration,chain>1?combo:collect,chain>1?.82f:.68f))AcquisitionPlays++;}
        public void PlayRefill(){if(Play(celebration,refill,.65f))RefillPlays++;}
        public void SetMuted(bool muted){if(muted){if(celebration)celebration.Stop();if(actions)actions.Stop();}}
        private void Update(){if(!feedback.SoundEnabled)SetMuted(true);if(previous==cycle.Phase)return;previous=cycle.Phase;if(previous==ClawCycle.Stage.Preparing)Play(actions,grab,.38f);if(previous==ClawCycle.Stage.Closing)Play(actions,clamp,.30f);if(previous==ClawCycle.Stage.Opening)Play(actions,release,.30f);}
        private void OnApplicationPause(bool value){paused=value;if(value)SetMuted(true);previous=cycle?cycle.Phase:previous;}
        private void OnDisable()=>SetMuted(true);
    }
}
