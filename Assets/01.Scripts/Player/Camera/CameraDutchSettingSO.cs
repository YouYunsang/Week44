using UnityEngine;

[CreateAssetMenu(fileName = "CameraDutchSetting", menuName = "Game/Camera Dutch Setting")]
public class CameraDutchSettingSO : ScriptableObject
{
    [Header("Dutch")]
    [SerializeField] private float _targetDutch = 0.4f;

    [Header("Blend")]
    [SerializeField] private float _blendInSpeed = 6f;
    [SerializeField] private float _blendOutSpeed = 10f;

    public float TargetDutch => _targetDutch;
    public float BlendInSpeed => _blendInSpeed;
    public float BlendOutSpeed => _blendOutSpeed;
}