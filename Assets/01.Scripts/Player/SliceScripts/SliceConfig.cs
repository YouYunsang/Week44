using UnityEngine;

public class SliceConfig : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float _fragmentDestroyDelay = 3f;
    public float FragmentDestroyDelay => _fragmentDestroyDelay;
}
