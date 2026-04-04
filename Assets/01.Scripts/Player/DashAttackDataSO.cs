using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "DashAttackData", menuName = "Game/Dash Attack Data")]
public class DashAttackDataSO : ScriptableObject
{
    [Header("스택 설정")]
    [SerializeField] private int _maxStack = 3;
    [SerializeField] private float _stackRechargeTime = 3f; //스택 1개 충전 시간

    [Header("대시 설정")]
    [SerializeField] private float _minAttackRange = 2f;
    [SerializeField] private float _maxAttackRange = 10f;
    [SerializeField] private float _chargeSpeed = 3f;
    [SerializeField] private float _dashSpeed = 20f;
    [SerializeField] private float _dashTimeout = 1f;
    [SerializeField] private float _stopDistance = 1f;

    public int MaxStack => _maxStack;
    public float StackRechargeTime => _stackRechargeTime;
    public float MinAttackRange     => _minAttackRange;
    public float MaxAttackRange     => _maxAttackRange;
    public float ChargeSpeed        => _chargeSpeed;
    public float DashSpeed          => _dashSpeed;
    public float DashTimeout        => _dashTimeout;
    public float StopDistance       => _stopDistance;
}
