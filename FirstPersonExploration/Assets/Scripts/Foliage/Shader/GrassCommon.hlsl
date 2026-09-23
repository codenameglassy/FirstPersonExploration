// GrassCommon
// Responsibility: Shared material data and vertex math for the Game/Foliage/Grass shader. Wind,
// player push, echo ripples, distance fade and color variation are computed per clump from the
// instance origin, so every pass (lit, shadow, depth) deforms identically.
// Requires mesh UV.y = 0 at the root and 1 at the tip.
#ifndef GAME_FOLIAGE_GRASS_COMMON_INCLUDED
#define GAME_FOLIAGE_GRASS_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Must match MaxRipples in GrassInteractionDriver.cs.
#define GRASS_MAX_RIPPLES 4

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half _Cutoff;
    half4 _RootColor;
    half4 _TipColor;
    half4 _DryColor;
    half _GradientHeight;
    float _VariationScale;
    half _VariationStrength;
    half4 _ShadowTint;
    half _RampThreshold;
    half _RampSmoothness;
    half _BacklightStrength;
    half _BacklightPower;
    float _ReceiveShadows;
    float _WindAngle;
    float _WindStrength;
    float _WindSpeed;
    float _GustStrength;
    float _GustScale;
    float _GustSpeed;
    float _PushRadius;
    float _PushStrength;
    half _PushFlatten;
    float _RippleWidth;
    float _RippleStrength;
    half4 _RippleColor;
    half _RippleGlow;
    float _FadeStart;
    float _FadeEnd;
    half _FadeShrink;
CBUFFER_END

// Globals written by GrassInteractionDriver. Kept outside the material buffer on purpose.
float4 _GrassPusher;                                   // xyz position, w = 1 while a pusher exists
float4 _GrassRipples[GRASS_MAX_RIPPLES];               // xyz echo origin, w current front radius (m)
float _GrassRippleAmplitudes[GRASS_MAX_RIPPLES];       // 0 = slot inactive

struct GrassVertexData
{
    float3 positionWS;
    float heightWeight;
    float fade;
    float variation;
    float rippleGlow;
};

float GrassHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float GrassValueNoise(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    float a = GrassHash(cell);
    float b = GrassHash(cell + float2(1.0, 0.0));
    float c = GrassHash(cell + float2(0.0, 1.0));
    float d = GrassHash(cell + float2(1.0, 1.0));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float2 GrassSafeDirectionXZ(float3 v)
{
    return v.xz / max(length(v.xz), 0.0001);
}

// Call after UNITY_SETUP_INSTANCE_ID so the object matrix is this instance's matrix.
GrassVertexData GrassDisplace(float3 positionOS, float2 uv)
{
    GrassVertexData data;

    float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
    float3 positionWS = TransformObjectToWorld(positionOS);
    float height = saturate(uv.y);

    // Distance fade is per clump, so a whole clump shrinks and dithers out together.
    float distanceToCamera = distance(_WorldSpaceCameraPos.xyz, originWS);
    float fade = saturate((distanceToCamera - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
    positionWS = lerp(positionWS, originWS, fade * _FadeShrink);

    // Wind: a per-clump sway plus world-space gusts that roll across the field downwind.
    float angle = radians(_WindAngle);
    float2 windDirection = float2(cos(angle), sin(angle));
    float phase = GrassHash(originWS.xz) * 6.2831853;
    float sway = sin(_Time.y * _WindSpeed + phase);
    float gust = GrassValueNoise(originWS.xz * _GustScale - windDirection * (_Time.y * _GustSpeed));
    float2 offset = windDirection * (sway * _WindStrength * (0.5 + gust) + gust * _GustStrength);

    // Player push: clumps near the pusher lean away from it and flatten toward the ground.
    float3 fromPusher = originWS - _GrassPusher.xyz;
    float push = saturate(1.0 - length(fromPusher) / max(_PushRadius, 0.001)) * _GrassPusher.w;
    push *= push;
    offset += GrassSafeDirectionXZ(fromPusher) * (push * _PushStrength);

    // Echo ripples: a band around each wavefront pushes clumps outward and makes them glow.
    float glow = 0.0;
    [unroll]
    for (int i = 0; i < GRASS_MAX_RIPPLES; i++)
    {
        float4 ripple = _GrassRipples[i];
        float3 fromOrigin = originWS - ripple.xyz;
        float band = saturate(1.0 - abs(length(fromOrigin) - ripple.w) / max(_RippleWidth, 0.001));
        band = band * band * (3.0 - 2.0 * band) * _GrassRippleAmplitudes[i];
        offset += GrassSafeDirectionXZ(fromOrigin) * (band * _RippleStrength);
        glow += band;
    }

    positionWS.xz += offset * (height * height);
    positionWS.y = lerp(positionWS.y, originWS.y, push * _PushFlatten);

    data.positionWS = positionWS;
    data.heightWeight = height;
    data.fade = fade;
    data.variation = GrassValueNoise(originWS.xz * _VariationScale) * _VariationStrength;
    data.rippleGlow = saturate(glow);
    return data;
}

// Screen-space dither: fade 0 keeps every pixel, fade 1 clips every pixel. Stays on the opaque path.
void GrassDitherClip(float2 pixelPosition, float fade)
{
    float threshold = frac(52.9829189 * frac(dot(pixelPosition, float2(0.06711056, 0.00583715))));
    clip(threshold - fade);
}

#endif
