using UnityEngine;
namespace PickDoll
{
    [CreateAssetMenu(menuName="PickDoll/Prize Definition")]
    public sealed class PrizeDefinition : ScriptableObject
    {
        public string speciesId, displayName;
        public float mass=.1f;
        public Vector3 centerOfMass=new Vector3(0,.045f,0);
        public float placementRadius=.105f;
    }
}
