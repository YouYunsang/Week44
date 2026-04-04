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

// 세팅 변경
public struct OnSettingsChangedEvent : IEvent
{
    public SettingsData data;
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