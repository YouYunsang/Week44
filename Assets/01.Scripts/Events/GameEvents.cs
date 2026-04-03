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

public struct OnPlayerMoveStartedEvent : IEvent { }

public struct OnPlayerMoveStoppedEvent : IEvent { }