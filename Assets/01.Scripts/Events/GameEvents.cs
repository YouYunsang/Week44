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

#region Player Move
public struct OnPlayerMoveStartedEvent : IEvent { }
public struct OnPlayerMoveStoppedEvent : IEvent { }
#endregion

#region Player Dash Attack
// 대시 차징 시작
public struct OnDashChargeStartedEvent : IEvent { }

// 대시 차징 취소
public struct OnDashChargeCanceledEvent : IEvent { }

// 대시 돌진 시작
public struct OnDashStartedEvent : IEvent { }

// 대시 돌진 종료
public struct OnDashEndedEvent : IEvent { }

// 대시 공격 판정 실행
public struct OnDashStrikeEvent : IEvent { }
#endregion

#region Weapon Swing
// 무기 휘두르기 (슬라이스 방향 기반)
public struct OnWeaponSwingEvent : IEvent
{
    public Vector2 direction; // 카메라 공간 기준 정규화된 스윙 방향
}
#endregion