using System.Collections;
using UnityEngine;

/// <summary>
/// 슬라이스된 조각에 붙는 컴포넌트
/// 3초 뒤 사라짐
/// 0번 슬롯(겉 표면)은 SurfaceFadeAlpha로만 페이드
/// 1번 슬롯(단면)은 홀로그램 디졸브 연출
/// </summary>
public class SliceFragment : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Physics")]
    [SerializeField] private float extraGravityMultiplier = 1f;

    [Header("Scale Fade")]
    [SerializeField] private bool useScaleFade = true;
    [SerializeField] private float scaleMultiplierAtEnd = 0.92f;

    [Header("Surface Fade (Slot 0)")]
    [SerializeField] private string surfaceFadeAlphaProperty = "_SurfaceFadeAlpha";

    [Header("Cap Hologram Fade (Slot 1)")]
    [SerializeField] private float startBaseEmission = 0f;
    [SerializeField] private float endBaseEmission = 0.3f;
    [SerializeField] private float startRimIntensity = 0.8f;
    [SerializeField] private float endRimIntensity = 1.3f;
    [SerializeField] private float flickerStrength = 0.25f;
    [SerializeField] private float flickerSpeed = 26f;

    [Header("Shader Property Names")]
    [SerializeField] private string fragmentAlphaProperty = "_FragmentAlpha";
    [SerializeField] private string rimIntensityProperty = "_RimIntensity";
    [SerializeField] private string baseEmissionProperty = "_BaseEmission";
    [SerializeField] private string dissolveProperty = "_DissolveAmount";

    [Header("Optional FX")]
    [SerializeField] private ParticleSystem vanishFx;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock block;
    private Vector3 originalScale;
    private Rigidbody rb;
    private bool started;

    public void Init(float delay)
    {
        destroyDelay = delay > 0f ? delay : 3f;

        if (!started)
        {
            Cache();
            ResetShaderState();
            StartCoroutine(LifetimeRoutine());
            started = true;
        }
    }

    private void Awake()
    {
        Cache();
        ResetShaderState();
    }

    private void Cache()
    {
        if (cachedRenderers != null && cachedRenderers.Length > 0)
            return;

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
    }

    private void ResetShaderState()
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer rend = cachedRenderers[i];
            if (rend == null) continue;

            Material[] mats = rend.sharedMaterials;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null) continue;

                block.Clear();
                rend.GetPropertyBlock(block, m);

                // 0번 슬롯 = 겉 표면
                if (m == 0)
                {
                    if (mat.HasProperty(surfaceFadeAlphaProperty))
                        block.SetFloat(surfaceFadeAlphaProperty, 1f);
                }
                // 1번 슬롯 = 단면
                else if (m == 1)
                {
                    if (mat.HasProperty(fragmentAlphaProperty))
                        block.SetFloat(fragmentAlphaProperty, 1f);

                    if (mat.HasProperty(rimIntensityProperty))
                        block.SetFloat(rimIntensityProperty, startRimIntensity);

                    if (mat.HasProperty(baseEmissionProperty))
                        block.SetFloat(baseEmissionProperty, startBaseEmission);

                    if (mat.HasProperty(dissolveProperty))
                        block.SetFloat(dissolveProperty, 0f);
                }

                rend.SetPropertyBlock(block, m);
            }
        }

        transform.localScale = originalScale;
    }

    private void FixedUpdate()
    {
        if (rb != null && extraGravityMultiplier > 1f)
            rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f), ForceMode.Acceleration);
    }

    private IEnumerator LifetimeRoutine()
    {
        float holdTime = Mathf.Max(0f, destroyDelay - fadeDuration);
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            ApplyVisual(t);
            yield return null;
        }

        ApplyVisual(1f);

        if (vanishFx != null)
        {
            vanishFx.transform.SetParent(null);
            vanishFx.Play();
            Destroy(vanishFx.gameObject, 2f);
        }

        Destroy(gameObject);
    }

    private void ApplyVisual(float t)
    {
        float surfaceAlpha = Mathf.Lerp(1f, 0f, t);
        float capAlpha = Mathf.Lerp(1f, 0f, t);

        float flicker = (Mathf.Sin(Time.time * flickerSpeed) * 0.5f + 0.5f) * flickerStrength;
        float baseEmission = Mathf.Lerp(startBaseEmission, endBaseEmission, t) + flicker;
        float rimIntensity = Mathf.Lerp(startRimIntensity, endRimIntensity, t) + flicker * 2f;

        if (useScaleFade)
        {
            float scaleMul = Mathf.Lerp(1f, scaleMultiplierAtEnd, t);
            transform.localScale = originalScale * scaleMul;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer rend = cachedRenderers[i];
            if (rend == null) continue;

            Material[] mats = rend.sharedMaterials;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null) continue;

                block.Clear();
                rend.GetPropertyBlock(block, m);

                // 0번 슬롯 = 겉 표면
                if (m == 0)
                {
                    if (mat.HasProperty(surfaceFadeAlphaProperty))
                        block.SetFloat(surfaceFadeAlphaProperty, surfaceAlpha);
                }
                // 1번 슬롯 = 단면
                else if (m == 1)
                {
                    if (mat.HasProperty(fragmentAlphaProperty))
                        block.SetFloat(fragmentAlphaProperty, capAlpha);

                    if (mat.HasProperty(rimIntensityProperty))
                        block.SetFloat(rimIntensityProperty, rimIntensity);

                    if (mat.HasProperty(baseEmissionProperty))
                        block.SetFloat(baseEmissionProperty, baseEmission);

                    if (mat.HasProperty(dissolveProperty))
                        block.SetFloat(dissolveProperty, t);
                }

                rend.SetPropertyBlock(block, m);
            }
        }
    }
}