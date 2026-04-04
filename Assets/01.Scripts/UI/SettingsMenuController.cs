using UnityEngine;
using UnityEngine.UI;

// PauseMenuController가 게임 일시정지/커서를 관리.
// 이 컨트롤러는 설정 패널 표시/동기화만 담당.
public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Button saveButton;
    [SerializeField] Button closeButton;

    float _snapBgm;
    float _snapSfx;
    float _snapSensitivity;
    bool  _snapFullscreen;
    bool  _hasSnapshot;

    void Awake()
    {
        canvas.enabled = false;
    }

    void Start()
    {
        bgmSlider.onValueChanged.AddListener(_         => PublishSettings());
        sfxSlider.onValueChanged.AddListener(_         => PublishSettings());
        sensitivitySlider.onValueChanged.AddListener(_ => PublishSettings());
        fullscreenToggle.onValueChanged.AddListener(_  => PublishSettings());
        saveButton.onClick.AddListener(OnSaveClicked);
        closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        SyncFromSettings();
        TakeSnapshot();
        canvas.enabled = true;
    }

    public void Close()
    {
        RestoreSnapshot();
        canvas.enabled = false;
    }

    void TakeSnapshot()
    {
        _snapBgm         = bgmSlider.value;
        _snapSfx         = sfxSlider.value;
        _snapSensitivity = sensitivitySlider.value;
        _snapFullscreen  = fullscreenToggle.isOn;
        _hasSnapshot     = true;
    }

    void RestoreSnapshot()
    {
        if (!_hasSnapshot) return;

        bgmSlider.SetValueWithoutNotify(_snapBgm);
        sfxSlider.SetValueWithoutNotify(_snapSfx);
        sensitivitySlider.SetValueWithoutNotify(_snapSensitivity);
        fullscreenToggle.SetIsOnWithoutNotify(_snapFullscreen);

        PublishSettings(); // 복원된 슬라이더 값으로 발행 → AudioManager·PlayerLook 자동 반영
        _hasSnapshot = false;
    }

    void PublishSettings()
    {
        EventBus<OnSettingsChangedEvent>.Publish(new OnSettingsChangedEvent
        {
            data = new SettingsData
            {
                bgmVolume        = bgmSlider.value,
                sfxVolume        = sfxSlider.value,
                mouseSensitivity = sensitivitySlider.value,
                fullscreen       = fullscreenToggle.isOn
            }
        });
    }

    void SyncFromSettings()
    {
        if (SettingsManager.Instance == null) return;
        SettingsData data = SettingsManager.Instance.Data;

        bgmSlider.SetValueWithoutNotify(data.bgmVolume);
        sfxSlider.SetValueWithoutNotify(data.sfxVolume);
        sensitivitySlider.SetValueWithoutNotify(
            Mathf.Clamp(data.mouseSensitivity, sensitivitySlider.minValue, sensitivitySlider.maxValue));
        fullscreenToggle.SetIsOnWithoutNotify(data.fullscreen);
    }

    void OnSaveClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetBGMVolume(bgmSlider.value);
            SettingsManager.Instance.SetSFXVolume(sfxSlider.value);
            SettingsManager.Instance.SetMouseSensitivity(sensitivitySlider.value);
            SettingsManager.Instance.SetFullscreen(fullscreenToggle.isOn);
            SettingsManager.Instance.Save();
        }
        _hasSnapshot = false;
        canvas.enabled = false;
    }
}
