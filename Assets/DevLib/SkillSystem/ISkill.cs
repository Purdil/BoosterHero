using UnityEngine;

namespace DevLib.SkillSystem
{
    public interface ISkill
    {
        SkillDataSO SkillData { get; }
        float NormalizedCooldown { get; } //0~1로 표현되는 쿨다운 값을 가지고 있어야 해
        bool IsUsing { get; }
        bool CanInterrupt { get; } //시전 중 캔슬(콤보/회피)이 가능한 윈도우인가. 기본은 false

        void InitializeSkill(ISkillModule skillModule); //스킬 모듈을 받아서 초기화하는 역할을 가져야해
        bool CanUseSkill(GameObject target = null);
        void UseSkill(GameObject target = null);
        void OnUpdateSkill();  //시전 중 매 프레임 호출 (차징 누적, 타이머 등)
        void OnReleaseInput(); //시전을 시작한 입력이 떼졌을 때 (차징 발사 등)
        void StopSkill(); //강제 종료
        void CleanUpSkillData();
    }
}