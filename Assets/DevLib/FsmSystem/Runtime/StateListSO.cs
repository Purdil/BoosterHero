using UnityEngine;

namespace DevLib.FsmSystem.Runtime
{
    [CreateAssetMenu(fileName = "State list", menuName = "Lib/Fsm/State list", order = 10)]
    public class StateListSO : ScriptableObject
    {
        [HideInInspector] public string generatePath;
        public string enumName;
        public StateSO[] states;
    }
}