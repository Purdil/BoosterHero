using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLib.FsmSystem.Runtime
{
    public class StateMachine
    {
        public AbstractState CurrentState { get; private set; }

        private Dictionary<int, AbstractState> _stateDict;

        public StateMachine(GameObject owner, StateSO[] stateList)
        {
            _stateDict = new Dictionary<int, AbstractState>();

            foreach (StateSO stateData in stateList)
            {
                Type type = Type.GetType(stateData.className);
                Debug.Assert(type != null , $"Finding type is null {stateData.className}");//안전코드
                AbstractState agentState = Activator.CreateInstance(type, owner, stateData) as AbstractState;
                
                _stateDict.Add(stateData.assetIndex, agentState);
            }
        }

        public void ChangeState(int newStateIndex)
        {
            CurrentState?.Exit();
            AbstractState newState = _stateDict.GetValueOrDefault(newStateIndex);
            Debug.Assert(newState != null, $"State is null : {newStateIndex}");

            CurrentState = newState;
            CurrentState.Enter();
        }

        public void UpdateMachine()
        {
            CurrentState?.Update();
        }
    }
}