using UnityEngine;

// 슬라이스 성공
public struct OnSliceEvent : IEvent
{
    public int score;
    public int combo;
}

// 게임 오버
public struct OnGameOverEvent : IEvent { }

// 게임 시작
public struct OnGameStartEvent : IEvent { }

// 타임스케일 변경
public struct OnTimeScaleChangedEvent : IEvent
{
    public float timeScale;
}

// 타임스케일 러프 요청
public struct OnLerpTimeScaleEvent : IEvent
{
    public float target;
    public float duration;
}

// 세팅 변경
public struct OnSettingsChangedEvent : IEvent
{
    public SettingsData data;
}

// 슬로우 게이지 변경
public struct OnSlowGaugeChangedEvent : IEvent
{
    public float current;
    public float max;
    public bool  isSlowing;
}

// 설정 메뉴 열림/닫힘
public struct OnMenuOpenEvent  : IEvent { }
public struct OnMenuCloseEvent : IEvent { }

#region Camera Noise Control
public enum CameraNoiseChannel
{
    Move,
    BossStomp
}
public struct OnCameraNoiseSignalEvent : IEvent
{
    public CameraNoiseChannel channel;
    public bool isActive;
    public float normalized;
}
#endregion

#region Camera Fov Control
public enum CameraFovChannel
{
    Move,
    Dash
}

public struct OnCameraFovSignalEvent : IEvent
{
    public CameraFovChannel channel;
    public bool isActive;
    public float normalized;
}
#endregion

#region Camera Motion Blur
public enum CameraMotionBlurChannel
{
    Dash
}

public struct OnCameraMotionBlurSignalEvent : IEvent
{
    public CameraMotionBlurChannel channel;
    public bool isActive;
    public float normalized;
}
#endregion

#region Camera Chromatic Channel
public enum CameraChromaticChannel
{
    Dash
}

public struct OnCameraChromaticSignalEvent : IEvent
{
    public CameraChromaticChannel channel;
    public bool isActive;
    public float normalized;
}
#endregion

#region Player Move
public struct OnPlayerMoveStartedEvent : IEvent { }

public struct OnPlayerMoveStoppedEvent : IEvent { }

public struct OnPlayerGroundedChangedEvent : IEvent
{
    public bool isGrounded;
}
#endregion

#region Player Dash Attack

// 대시 돌진 시작
public struct OnDashStartedEvent : IEvent { }

// 대시 돌진 종료
public struct OnDashEndedEvent : IEvent { }

// 대시 공격 판정 실행
public struct OnDashStrikeEvent : IEvent { }
#endregion

#region Landing
public enum LandingImpactType
{
    Small,
    Medium,
    Large
}

public struct OnPlayerLandedEvent : IEvent
{
    public LandingImpactType impactType;
    public float downwardSpeed;
    public float impulseStrength;
    public float impulseDuration;
}
#endregion

#region Boss
public enum LimbType  { Head, LeftArm, RightArm, LeftLeg, RightLeg, Torso }
public enum BossPhase { FullBody, ArmsOnly, CoreOnly }

// 팔다리가 잘렸을 때
public struct OnBossLimbSlicedEvent : IEvent
{
    public LimbType limb;
}

// 페이즈 전환
public struct OnBossPhaseChangedEvent : IEvent
{
    public BossPhase phase;
}

// 보스 공격 타이밍 (히트 여부 무관, 공격 모션 절정 시점)
public enum BossAttackType { Kick, JumpStomp, ArmSwing, ArmSlam, Roll }
public struct OnBossAttackEvent : IEvent
{
    public BossAttackType attackType;
}

// 보스 공격 히트 (플레이어 넉백 등에 사용)
public struct OnBossAttackHitEvent : IEvent
{
    public Vector3 direction;
    public float   force;
}

//보스 Jump Stomp 공격 임팩트
public struct OnBossStompImpactEvent : IEvent
{
    public Vector3 position;
}

// 보스 사망 (토르소 파괴)
public struct OnBossDiedEvent : IEvent { }
#endregion

#region Weapon Swing
public enum WeaponSwingType
{
    NormalAttack,
    DashAttack
}

// 무기 휘두르기 (슬라이스 방향 기반)
public struct OnWeaponSwingEvent : IEvent
{
    public Vector2 direction; // 카메라 공간 기준 정규화된 스윙 방향
    public WeaponSwingType swingType;
}
#endregion