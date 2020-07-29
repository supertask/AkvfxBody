// Spiralizer effect custom shader
// https://github.com/keijiro/TestbedHDRP

using UnityEngine;
using UnityEngine.Playables;

[ExecuteInEditMode]
public sealed class Spiralizer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField, Range(0, 1)] float _density = 0.05f;
    [SerializeField] float _size = 0.05f;

    [SerializeField] float _inflation = 1;
    [SerializeField] float _rotation = 1;
    [SerializeField] Transform _origin = null;

    [SerializeField, ColorUsage(false, true)] Color _emissionColor = Color.black;
    [SerializeField, ColorUsage(false, true)] Color _edgeColor = Color.white;
    [SerializeField, Range(0, 8)] float _edgeWidth = 1;
    [SerializeField, Range(0, 1)] float _hueShift = 0;
    [SerializeField, Range(0, 1)] float _highlight = 0.2f;

    [SerializeField] Renderer[] _renderers = null;

    void OnValidate()
    {
        _size = Mathf.Max(0, _size);
        _inflation = Mathf.Max(0, _inflation);
    }

    #endregion

    #region Utility properties and methods for internal use
    Vector4 EffectPlane
    {
        get
        {
            var fwd = transform.forward / transform.localScale.z;
            var dist = Vector3.Dot(fwd, transform.position);
            return new Vector4(fwd.x, fwd.y, fwd.z, dist);
        }
    }

    Vector4 ColorToHsvm(Color color)
    {
        var max = Mathf.Max(color.maxColorComponent, 1e-5f);
        float h, s, v;
        Color.RGBToHSV(color / max, out h, out s, out v);
        return new Vector4(h, s, v, max);
    }

    #endregion

    #region Shader property IDs

    static class ShaderIDs
    {
        public static readonly int BaseParams = Shader.PropertyToID("_BaseParams");
        public static readonly int AnimParams = Shader.PropertyToID("_AnimParams");
        public static readonly int TimeParams = Shader.PropertyToID("_TimeParams");
        public static readonly int EffOrigin = Shader.PropertyToID("_EffOrigin");
        public static readonly int EffSpace = Shader.PropertyToID("_EffSpace");
        public static readonly int EffPlaneC = Shader.PropertyToID("_EffPlaneC");
        public static readonly int EffPlaneP = Shader.PropertyToID("_EffPlaneP");
        public static readonly int EffHSVM = Shader.PropertyToID("_EffHSVM");
        public static readonly int EdgeHSVM = Shader.PropertyToID("_EdgeHSVM");
        public static readonly int EdgeWidth = Shader.PropertyToID("_EdgeWidth");
        public static readonly int HueShift = Shader.PropertyToID("_HueShift");
    }
    #endregion


    #region MonoBehaviour implementation

    MaterialPropertyBlock _sheet;
    Vector4 _prevEffectPlane = Vector3.one * 1e+5f;
    float _prevTime = 1e+5f;

    void LateUpdate()
    {
        if (_renderers == null || _renderers.Length == 0) return;

        if (_sheet == null) _sheet = new MaterialPropertyBlock();

        var plane = EffectPlane;

        // Filter out large deltas.
        if ((_prevEffectPlane - plane).magnitude > 100) _prevEffectPlane = plane;

        var bparams = new Vector3(_density, _size, _highlight);
        var aparams = new Vector3(_inflation, _rotation);
        var espace = _origin != null ? _origin.worldToLocalMatrix : Matrix4x4.identity;
        var emission = ColorToHsvm(_emissionColor);
        var edge = ColorToHsvm(_edgeColor);

        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            var espace_obj = renderer.transform.localToWorldMatrix * espace;
            renderer.GetPropertyBlock(_sheet);
            _sheet.SetVector(ShaderIDs.BaseParams, bparams);
            _sheet.SetVector(ShaderIDs.AnimParams, aparams);
            _sheet.SetMatrix(ShaderIDs.EffSpace, espace_obj);
            _sheet.SetVector(ShaderIDs.EffPlaneC, plane);
            _sheet.SetVector(ShaderIDs.EffPlaneP, _prevEffectPlane);
            _sheet.SetVector(ShaderIDs.EffHSVM, emission);
            _sheet.SetColor(ShaderIDs.EdgeHSVM, edge);
            _sheet.SetFloat(ShaderIDs.EdgeWidth, _edgeWidth);
            _sheet.SetFloat(ShaderIDs.HueShift, _hueShift);
            renderer.SetPropertyBlock(_sheet);
        }

        _prevEffectPlane = plane;
    }

    #endregion

}
