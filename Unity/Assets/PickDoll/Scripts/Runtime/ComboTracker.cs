using System.Collections.Generic;
using UnityEngine;
namespace PickDoll
{
    public sealed class ComboChain
    {
        public const double Window=15;
        private readonly HashSet<string> seen=new HashSet<string>();
        private double last=double.NegativeInfinity;
        public int Count {get;private set;}
        public double Remaining(double now)=>System.Math.Max(0,Window-(now-last));
        public int Accept(string ticket,double now){
            if(string.IsNullOrEmpty(ticket)||!seen.Add(ticket))return Count;
            Count=Count>0&&now-last<=Window?Count+1:1;last=now;return Count;
        }
        public void Reset(){Count=0;last=double.NegativeInfinity;}
    }
    public sealed class ComboTracker : MonoBehaviour
    {
        public PrizeReceiver receiver;
        public readonly ComboChain Chain=new ComboChain();
        public float Remaining=>(float)Chain.Remaining(Time.realtimeSinceStartupAsDouble);
        public int Register(PrizeIdentity prize)=>prize.Delivered?Chain.Accept(prize.Ticket,Time.realtimeSinceStartupAsDouble):Chain.Count;
        private void OnEnable(){receiver.Acquired+=OnAcquired;receiver.RoundStarted+=ResetChain;}
        private void OnDisable(){receiver.Acquired-=OnAcquired;receiver.RoundStarted-=ResetChain;ResetChain();}
        private void OnAcquired(PrizeIdentity prize)=>Register(prize);
        public void ResetChain()=>Chain.Reset();
        private void OnApplicationPause(bool paused){if(paused)ResetChain();}
        private void OnApplicationFocus(bool focused){if(!focused)ResetChain();}
    }
}
