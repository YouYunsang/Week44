using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] InputSO inputSO;
    [SerializeField] Canvas canvas;
    [SerializeField] Button resumeButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;
    [SerializeField] SettingsMenuController settingsMenu;

    void Awake()
    {
        canvas.enabled = false;
    }

    void OnEnable()
    {
        if (inputSO != null) inputSO.OnStop += ToggleMenu;
    }

    void OnDisable()
    {
        if (inputSO != null) inputSO.OnStop -= ToggleMenu;
    }

    void Start()
    {
        resumeButton.onClick.AddListener(Close);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(OnQuit);
    }

    void ToggleMenu()
    {
        if (canvas.enabled) Close();
        else Open();
    }

    public void Open()
    {
        canvas.enabled = true;
        TimeManager.Instance?.Pause();
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        EventBus<OnMenuOpenEvent>.Publish(new OnMenuOpenEvent());
    }

    public void Close()
    {
        settingsMenu?.Close();
        canvas.enabled = false;
        TimeManager.Instance?.Resume();
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
        EventBus<OnMenuCloseEvent>.Publish(new OnMenuCloseEvent());
    }

    void OpenSettings()
    {
        settingsMenu?.Open();
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
