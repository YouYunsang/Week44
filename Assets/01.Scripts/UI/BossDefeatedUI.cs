using UnityEngine;

public class BossDefeatedUI : MonoBehaviour
{
    [SerializeField] private GameObject _uiObject;

    private void OnEnable()
    {
        EventBus<OnSliceableReadyEvent>.Subscribe(OnSliceableReady);
        EventBus<OnBossDiedEvent>.Subscribe(OnBossDied);
    }

    private void OnDisable()
    {
        EventBus<OnSliceableReadyEvent>.Unsubscribe(OnSliceableReady);
        EventBus<OnBossDiedEvent>.Unsubscribe(OnBossDied);
    }

    // SliceableMeshSceneChanger 셋업 완료 → UI 끔
    private void OnSliceableReady(OnSliceableReadyEvent e)
    {
        if (_uiObject != null)
            _uiObject.SetActive(false);
    }

    // 보스 처치 → UI 켬
    private void OnBossDied(OnBossDiedEvent e)
    {
        if (_uiObject != null)
            _uiObject.SetActive(true);
    }
}
