using UnityEngine;

namespace DevLib.FsmSystem.Runtime
{
    public abstract class AbstractState
    {
        protected GameObject _owner;
        protected StateSO _stateSO;
        public AbstractState(GameObject owner, StateSO stateSO)
        {
            _owner = owner;    
            _stateSO = stateSO;
        }
        
        public virtual void Enter() {}

        // Update는 템플릿. 실제 프레임 로직은 OnUpdate에 작성한다.
        public void Update() => OnUpdate();

        // 매 프레임 로직. 반환값 true = "내가 상태 전이를 일으켰으니 이후 로직을 멈춰라".
        // 계약: 오버라이드는 반드시 base.OnUpdate()를 먼저 호출하고, true면 즉시 true를 반환한다.
        //       ChangeState를 호출한 직후에는 항상 true를 반환한다.
        protected virtual bool OnUpdate() => false;

        public virtual void Exit() {}
    }
}