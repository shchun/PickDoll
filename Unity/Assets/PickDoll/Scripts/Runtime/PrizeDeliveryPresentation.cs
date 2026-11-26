using System.Collections;
using UnityEngine;
namespace PickDoll
{
    public sealed class PrizeDeliveryPresentation : MonoBehaviour
    {
        public PrizeReceiver receiver;
        public Transform skin;
        public float holdSeconds=1.4f;
        public bool Hidden {get;private set;}
        private PrizeIdentity identity;
        private Rigidbody body;
        private Collider[] colliders;
        private void Awake(){identity=GetComponent<PrizeIdentity>();body=GetComponent<Rigidbody>();colliders=GetComponentsInChildren<Collider>(true);}
        private void OnEnable(){receiver.Acquired+=OnAcquired;identity.Rearmed+=ResetPresentation;}
        private void OnDisable(){receiver.Acquired-=OnAcquired;identity.Rearmed-=ResetPresentation;StopAllCoroutines();}
        private void OnAcquired(PrizeIdentity prize){if(prize==identity)StartCoroutine(ClearPickupBay(prize.Ticket));}
        private IEnumerator ClearPickupBay(string ticket){
            yield return new WaitForSeconds(holdSeconds);
            if(!identity.Delivered||identity.Ticket!=ticket)yield break;
            // Only a committed prize is removed. Pending prizes retain their physical path.
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;
            foreach(var collider in colliders)collider.enabled=false;
            skin.gameObject.SetActive(false);Hidden=true;
        }
        private void ResetPresentation(){StopAllCoroutines();Hidden=false;skin.gameObject.SetActive(true);foreach(var collider in colliders)collider.enabled=true;}
    }
}
