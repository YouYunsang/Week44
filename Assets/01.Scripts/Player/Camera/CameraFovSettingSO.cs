using UnityEngine;

[CreateAssetMenu(fileName = "CameraFovSetting", menuName = "Game/Camera FOV Setting")]
public class CameraFovSettingSO : ScriptableObject
{
    [Header("FOV")]
    [SerializeField] private float _targetFov = 60f;

    [Header("Blend")]
    [SerializeField] private float _blendInTime = 0.12f;
    [SerializeField] private float _blendOutTime = 0.18f;

    public float TargetFov => _targetFov;
    public float BlendInTime => _blendInTime;
    public float BlendOutTime => _blendOutTime;
}