// DissolveController.cs
// Drives _DissolveAmount on a group of renderers (MeshRenderer or SkinnedMeshRenderer) via a MaterialPropertyBlock,
// so objects sharing one dissolve material animate independently. Mode-agnostic: 0 = intact / statue, 1 = dissolved / original.
// The component enables itself only while animating, so idle instances cost nothing per frame.
using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DissolveController : MonoBehaviour
{
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    [SerializeField] private Renderer[] _renderers = Array.Empty<Renderer>();
    [SerializeField, Min(0.01f)] private float _duration = 1.5f;
    [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _initialAmount;
    [Tooltip("Clip mode materials only. Disables the renderers at Amount 1 so a fully dissolved object costs no draw calls.")]
    [SerializeField] private bool _disableRenderersWhenFullyDissolved;

    private MaterialPropertyBlock _block;
    private float _amount;
    private float _from;
    private float _to;
    private float _elapsed;
    private float _playDuration;
    private bool _isPlaying;

    public event Action DissolveCompleted;
    public event Action RestoreCompleted;

    public float Amount => _amount;
    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        ApplyAmount(_initialAmount);
        enabled = false;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _playDuration);
        ApplyAmount(Mathf.Lerp(_from, _to, _curve.Evaluate(t)));

        if (t >= 1f)
        {
            Finish();
        }
    }

    public void Dissolve()
    {
        PlayTo(1f);
    }

    public void Restore()
    {
        PlayTo(0f);
    }

    public void SetAmountImmediate(float amount)
    {
        _isPlaying = false;
        enabled = false;
        ApplyAmount(Mathf.Clamp01(amount));
    }

    private void PlayTo(float target)
    {
        _from = _amount;
        _to = target;
        _elapsed = 0f;
        // Scale duration by remaining distance so reversing mid-transition keeps a consistent speed.
        _playDuration = _duration * Mathf.Abs(_to - _from);

        if (_playDuration <= 0f)
        {
            ApplyAmount(_to);
            Finish();
            return;
        }

        _isPlaying = true;
        enabled = true;
    }

    private void Finish()
    {
        _isPlaying = false;
        enabled = false;

        if (_to >= 1f)
        {
            DissolveCompleted?.Invoke();
        }
        else
        {
            RestoreCompleted?.Invoke();
        }
    }

    private void ApplyAmount(float amount)
    {
        _block ??= new MaterialPropertyBlock();
        _amount = amount;

        bool visible = !_disableRenderersWhenFullyDissolved || amount < 1f;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer targetRenderer = _renderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(_block);
            _block.SetFloat(DissolveAmountId, amount);
            targetRenderer.SetPropertyBlock(_block);

            if (targetRenderer.enabled != visible)
            {
                targetRenderer.enabled = visible;
            }
        }
    }

    // Editor-only convenience: auto-fills renderers when the component is added.
    private void Reset()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }
}
