using UnityEngine;

    /// <summary>
    /// 슬라이스된 조각에 붙는 컴포넌트
    /// 일정 시간 후 자동으로 제거됨
    /// </summary>
public class SliceFragment : MonoBehaviour
{
    private float _destroyDelay = 3f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init(float delay)
    {
        _destroyDelay = delay;
        Destroy(gameObject, _destroyDelay);
    }
}
