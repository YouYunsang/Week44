using UnityEngine;

public class BossDefeatedUI : MonoBehaviour
{
    [SerializeField] private GameObject _uiObject;

    private void OnEnable()  => EventBus<OnBossDiedEvent>.Subscribe(OnBossDied);
    private void OnDisable() => EventBus<OnBossDiedEvent>.Unsubscribe(OnBossDied);

    private void Start()
    {
        if (_uiObject != null)
            _uiObject.SetActive(false);
    }

    private void OnBossDied(OnBossDiedEvent e)
    {
        if (_uiObject != null)
            _uiObject.SetActive(true);
    }
}
