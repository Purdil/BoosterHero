using _Project._Scripts.Core;
using DevLib.ModuleSystem;
using UnityEngine;

namespace _Project._Scripts.Player
{
    public class PlayerMovementModule : MonoBehaviour, IModule
    {
        [SerializeField] private PlayerInputSO inputSO;
        private Agent _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
        }
        
        
        
        
    }
}