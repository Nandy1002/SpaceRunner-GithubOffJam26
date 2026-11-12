using UnityEngine;

namespace SpaceRunner
{
    // Optional prototype prefab for the team's existing eight-direction gravity menu.
    public sealed class GravityMenuBinding : MonoBehaviour
    {
        [SerializeField] private GravityChangerUI menu;
        [SerializeField] private GravityManager manager;
        public GravityChangerUI Menu => menu;
        public void Initialize(FirstPersonController player) => manager.BindPlayer(player);
    }
}
