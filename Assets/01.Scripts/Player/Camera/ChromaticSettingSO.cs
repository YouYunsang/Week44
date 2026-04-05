using UnityEngine;

[CreateAssetMenu(menuName = "Game/Camera Chromatic Setting")]
public class CameraChromaticSettingSO : ScriptableObject
{
    [SerializeField] private float _intensity = 0.2f;

    [SerializeField] private float _blendInSpeed = 10f;
    [SerializeField] private float _blendOutSpeed = 15f;

    public float Intensity => _intensity;
    public float BlendInSpeed => _blendInSpeed;
    public float BlendOutSpeed => _blendOutSpeed;
}