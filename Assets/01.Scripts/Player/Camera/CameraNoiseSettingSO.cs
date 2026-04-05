using UnityEngine;
using Unity.Cinemachine;

[CreateAssetMenu(fileName = "CameraNoiseSetting", menuName = "Game/Camera Noise Setting")]
public class CameraNoiseSettingSO : ScriptableObject
{
    [Header("Noise Profile")]
    [SerializeField] private NoiseSettings _noiseProfile;

    [Header("Noise Values")]
    [SerializeField] private float _amplitudeGain = 0f;
    [SerializeField] private float _frequencyGain = 1f;
    [SerializeField] private Vector3 _pivotOffset = Vector3.zero;

    [Header("Blend")]
    [SerializeField] private float _blendInSpeed = 8f;
    [SerializeField] private float _blendOutSpeed = 10f;

    public NoiseSettings NoiseProfile => _noiseProfile;
    public float AmplitudeGain => _amplitudeGain;
    public float FrequencyGain => _frequencyGain;
    public Vector3 PivotOffset => _pivotOffset;
    public float BlendInSpeed => _blendInSpeed;
    public float BlendOutSpeed => _blendOutSpeed;
}
