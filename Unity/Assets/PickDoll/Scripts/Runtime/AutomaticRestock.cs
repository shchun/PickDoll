using System.Linq;
using UnityEngine;
namespace PickDoll
{
    public sealed class AutomaticRestock : MonoBehaviour
    {
        public ClawCycle cycle;
        public AcquisitionFeedback feedback;
        public int RefillCount {get;private set;}
        public bool WaitingForDelivery {get;private set;}
        private float readyTime;
        public void Tick(float dt){
            if(Time.timeScale<=0||cycle.Busy){readyTime=0;return;}
            bool allCollected=cycle.prizes.All(p=>p.GetComponent<PrizeIdentity>().Delivered);
            WaitingForDelivery=!allCollected&&!cycle.prizes.Any(p=>!p.GetComponent<PrizeIdentity>().Delivered&&p.position.y>.60f&&Mathf.Abs(p.position.x)<.5f&&Mathf.Abs(p.position.z)<.46f);
            bool cleared=allCollected&&cycle.prizes.All(p=>p.GetComponent<PrizeDeliveryPresentation>().Hidden);
            if(!cleared||feedback.HasPending){readyTime=0;return;}
            readyTime+=dt;if(readyTime<.5f)return;
            cycle.machine.ResetPosition("automatic_refill");readyTime=0;WaitingForDelivery=false;RefillCount++;feedback.ShowRestock();
            Debug.Log("PICKDOLL_AUTO_RESTOCK count="+RefillCount+" collected="+cycle.receiver.Count);
        }
        private void Update()=>Tick(Time.deltaTime);
    }
}
