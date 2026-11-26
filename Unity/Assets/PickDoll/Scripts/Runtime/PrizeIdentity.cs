using System;
using UnityEngine;
namespace PickDoll
{
    public sealed class PrizeIdentity : MonoBehaviour
    {
        public string displayName = "Bear";
        public PrizeDefinition definition;
        public string SpeciesId => definition ? definition.speciesId : "bear";
        public string Ticket { get; private set; }
        public bool EnteredChute { get; internal set; }
        public bool Delivered { get; internal set; }
        public event Action Rearmed;
        public void EnsureTicket(){if(string.IsNullOrEmpty(Ticket))Rearm();}
        public void Rearm(){Ticket=Guid.NewGuid().ToString("N");EnteredChute=false;Delivered=false;Rearmed?.Invoke();}
    }
}
