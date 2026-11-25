using UnityEngine;
namespace PickDoll
{
    // Attached only to the final tray floor, never the ramp or guards.
    public sealed class PickupFloorContact : MonoBehaviour
    {
        public PrizeReceiver receiver;
        private void OnCollisionEnter(Collision collision)=>Check(collision);
        private void OnCollisionStay(Collision collision)=>Check(collision);
        private void Check(Collision collision)
        {
            if(!collision.rigidbody)return;
            var prize=collision.rigidbody.GetComponent<PrizeIdentity>();
            if(!prize)return;
            for(int i=0;i<collision.contactCount;i++) {
                var contact=collision.GetContact(i);
                // Accept the top face only; side/lip contact is not a completed delivery.
                if(contact.point.y >= receiver.pickupFloor.bounds.max.y-.008f && Mathf.Abs(contact.normal.y)>.5f) {
                    receiver.TouchPickupFloor(prize);return;
                }
            }
        }
    }
}
