using System;
using UnityEngine;

namespace DevLib.SkillSystem
{
    public abstract class AbstractSkill : MonoBehaviour, ISkill
    {
        public event Action<AbstractSkill> OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }
        
        
        public float NormalizedCooldown
        {
            get
            {
                if (SkillData == null || SkillData.cooldown <= 0f) return 0f;
                return Mathf.Clamp01(1f - (Time.time - _lastUsedTime) / SkillData.cooldown);
            }
        }
        // 쿨다운 진행 중 여부. (마지막 사용 시점 + cooldown이 지나지 않았으면 true)
        public bool IsOnCooldown => NormalizedCooldown > 0f;
        public bool IsUsing { get; private set; }

        // 기본 스킬은 캔슬 윈도우가 없다. 차징/콤보 스킬이 애니메이션 이벤트 등으로 열어준다.
        public virtual bool CanInterrupt => false;

        protected AbstractSkillModule _skillModule;
        protected float _lastUsedTime = float.NegativeInfinity;
        
        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            _skillModule = skillModule as AbstractSkillModule;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
        }

        // 시전 중 매 프레임 호출. 즉발 스킬은 비워두고, 차징/지속 스킬이 진행 로직을 채운다.
        public virtual void OnUpdateSkill() { }

        // 시전을 시작한 입력이 떼졌을 때 호출. 차징 스킬이 여기서 발사한다.
        public virtual void OnReleaseInput() { }

        public void StopSkill()
        {
            CleanUpSkillData();
        }

        public virtual void CleanUpSkillData()
        {
            _lastUsedTime = Time.time;
            IsUsing = false;
            OnSkillEnd?.Invoke(this);
        }
    }
}