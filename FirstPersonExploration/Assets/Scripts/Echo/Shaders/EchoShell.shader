// EchoShell
// Responsibility: URP transparent shell for echo wavefronts. Fresnel rim, a depth-based contact line
// where the shell cuts through geometry, noise shimmer and vertex wobble. Refraction is optional and
// off by default because it needs the URP Opaque Texture, a full-screen copy every frame.
Shader "Game/Echo/Shell"
{
    Properties
    {
        [HDR] _Color            ("Echo color", Color) = (0.85, 0.93, 1.0, 1)
        _RimPower               ("Rim power", Range(0.5, 8)) = 3.0
        _RimStrength            ("Rim strength", Range(0, 4)) = 1.15
        _BodyAlpha              ("Body haze", Range(0, 0.5)) = 0.045
        _ContactSoftness        ("Contact softness", Range(0.01, 3)) = 0.45
        _ContactStrength        ("Contact strength", Range(0, 4)) = 1.6
        [Toggle(_ECHO_REFRACTION)] _Refraction ("Refraction (needs Opaque Texture)", Float) = 0
        _Distort                ("Refraction amount", Range(0, 0.1)) = 0.02
        _NoiseScale             ("Noise scale", Range(0.5, 20)) = 6.0
        _NoiseSpeed             ("Noise speed", Range(0, 4)) = 0.6
        _NoiseStrength          ("Noise strength", Range(0, 1)) = 0.45
        _Wobble                 ("Wobble amount", Range(0, 0.15)) = 0.03
        _WobbleScale            ("Wobble scale", Range(0.5, 8)) = 2.2
        [HideInInspector] _Life  ("Life 01", Range(0, 1)) = 0
        [HideInInspector] _Alpha ("Master alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+50"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "EchoShell"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _ECHO_REFRACTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _RimPower;
                float  _RimStrength;
                float  _BodyAlpha;
                float  _ContactSoftness;
                float  _ContactStrength;
                float  _Distort;
                float  _NoiseScale;
                float  _NoiseSpeed;
                float  _NoiseStrength;
                float  _Wobble;
                float  _WobbleScale;
                float  _Life;
                float  _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float4 screenPos  : TEXCOORD3;
            };

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float vnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(i + float3(0, 0, 0));
                float n100 = hash13(i + float3(1, 0, 0));
                float n010 = hash13(i + float3(0, 1, 0));
                float n110 = hash13(i + float3(1, 1, 0));
                float n001 = hash13(i + float3(0, 0, 1));
                float n101 = hash13(i + float3(1, 0, 1));
                float n011 = hash13(i + float3(0, 1, 1));
                float n111 = hash13(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                float w = vnoise(v.positionOS.xyz * _WobbleScale + float3(_Time.y * 0.35, 0, 0)) * 2.0 - 1.0;
                float3 posOS = v.positionOS.xyz + normalize(v.normalOS) * w * _Wobble;

                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vn = GetVertexNormalInputs(v.normalOS);

                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS   = vn.normalWS;
                o.positionOS = v.positionOS.xyz;
                o.screenPos  = ComputeScreenPos(vp.positionCS);
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float3 N = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));

                float fres = pow(1.0 - saturate(dot(N, V)), _RimPower);

                float2 uv = i.screenPos.xy / max(i.screenPos.w, 1e-5);

                float n = vnoise(i.positionOS * _NoiseScale + float3(0, _Time.y * _NoiseSpeed, 0));
                float shimmer = lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, n);

                float sceneEye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float fragEye  = i.screenPos.w;
                float contact  = 1.0 - saturate((sceneEye - fragEye) / max(_ContactSoftness, 1e-3));
                contact *= contact;

                float rim  = fres * _RimStrength * shimmer;
                float glow = rim + contact * _ContactStrength;
                float thin = lerp(1.0, 0.55, _Life);
                float a    = saturate((glow + _BodyAlpha) * thin * _Alpha);

                float3 emissive = _Color.rgb * glow;

            #if defined(_ECHO_REFRACTION)
                float3 Nv   = TransformWorldToViewDir(N);
                float2 ruv  = saturate(uv + Nv.xy * _Distort * fres);
                float3 refr = SampleSceneColor(ruv);
                return half4((refr + emissive) * a, a);
            #else
                // Alpha 0 under this blend mode is purely additive: glow without darkening the scene.
                return half4(emissive * a, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    Fallback Off
}
