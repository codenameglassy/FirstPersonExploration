// Grass
// Responsibility: Instanced URP grass shader for GrassFieldRenderer. Wind sway and rolling gusts,
// player push, echo wavefront ripples with glow, dithered distance fade with shrink, root-to-tip gradient with root color matched to the terrain,
// world-space dry patch variation, toon ramp lighting and backlight glow toward the sun.
// Alpha clipped, two sided, opaque queue. Shared math lives in GrassCommon.hlsl.
Shader "Game/Foliage/Grass"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Color)]
        _RootColor ("Root Color (match terrain)", Color) = (0.20, 0.28, 0.12, 1)
        _TipColor ("Tip Color", Color) = (0.55, 0.75, 0.30, 1)
        _DryColor ("Dry Tip Color", Color) = (0.80, 0.72, 0.38, 1)
        _GradientHeight ("Gradient Height", Range(0.05, 1)) = 0.6
        _VariationScale ("Variation Scale", Float) = 0.05
        _VariationStrength ("Variation Strength", Range(0, 1)) = 0.5

        [Header(Lighting)]
        _ShadowTint ("Shadow Tint", Color) = (0.55, 0.60, 0.70, 1)
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
        _RampSmoothness ("Ramp Smoothness", Range(0.001, 0.5)) = 0.1
        _BacklightStrength ("Backlight Strength", Range(0, 2)) = 0.6
        _BacklightPower ("Backlight Power", Range(1, 16)) = 4
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows ("Receive Shadows", Float) = 1

        [Header(Wind)]
        _WindAngle ("Wind Angle", Range(0, 360)) = 45
        _WindStrength ("Sway Strength", Float) = 0.08
        _WindSpeed ("Sway Speed", Float) = 2
        _GustStrength ("Gust Strength", Float) = 0.2
        _GustScale ("Gust Scale", Float) = 0.08
        _GustSpeed ("Gust Speed", Float) = 3

        [Header(Player Push)]
        _PushRadius ("Push Radius", Float) = 1
        _PushStrength ("Push Strength", Float) = 0.5
        _PushFlatten ("Push Flatten", Range(0, 1)) = 0.4

        [Header(Echo Ripple)]
        _RippleWidth ("Ripple Width", Float) = 1.5
        _RippleStrength ("Ripple Strength", Float) = 0.35
        [HDR] _RippleColor ("Ripple Color", Color) = (1.6, 1.5, 1.2, 1)
        _RippleGlow ("Ripple Glow", Range(0, 4)) = 1

        [Header(Fade)]
        _FadeStart ("Fade Start", Float) = 25
        _FadeEnd ("Fade End (match Draw Distance)", Float) = 35
        _FadeShrink ("Fade Shrink", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF

            #include "GrassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                float4 grassData : TEXCOORD3; // x height, y fade, z variation, w fog factor
                half rippleGlow : TEXCOORD4;
            };

            Varyings ForwardVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                GrassVertexData grass = GrassDisplace(input.positionOS.xyz, input.uv);

                output.positionWS = grass.positionWS;
                output.positionCS = TransformWorldToHClip(grass.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.grassData = float4(grass.heightWeight, grass.fade, grass.variation, ComputeFogFactor(output.positionCS.z));
                output.rippleGlow = grass.rippleGlow;
                return output;
            }

            half4 ForwardFrag(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texel.a - _Cutoff);
                GrassDitherClip(input.positionCS.xy, input.grassData.y);

                half height = input.grassData.x;
                half3 tipColor = lerp(_TipColor.rgb, _DryColor.rgb, input.grassData.z);
                half3 tint = lerp(_RootColor.rgb, tipColor, smoothstep(0.0, _GradientHeight, height));
                half3 albedo = texel.rgb * tint;

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 normalWS = normalize(input.normalWS);

                half halfLambert = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                half ramp = smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, halfLambert);
                half lit = ramp * mainLight.shadowAttenuation;
                half3 direct = lerp(_ShadowTint.rgb, half3(1.0, 1.0, 1.0), lit) * mainLight.color;
                half3 ambient = SampleSH(normalWS);

                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                half backlight = pow(saturate(dot(-viewDirection, mainLight.direction)), _BacklightPower)
                    * _BacklightStrength * height * mainLight.shadowAttenuation;

                half3 color = albedo * (direct + ambient) + albedo * mainLight.color * backlight;
                color += _RippleColor.rgb * (input.rippleGlow * _RippleGlow * (0.35 + 0.65 * height));
                color = MixFog(color, input.grassData.w);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "GrassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                GrassVertexData grass = GrassDisplace(input.positionOS.xyz, input.uv);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - grass.positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(grass.positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fade = grass.fade;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(alpha - _Cutoff);
                GrassDitherClip(input.positionCS.xy, input.fade);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #pragma multi_compile_instancing

            #include "GrassCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                GrassVertexData grass = GrassDisplace(input.positionOS.xyz, input.uv);
                output.positionCS = TransformWorldToHClip(grass.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fade = grass.fade;
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(alpha - _Cutoff);
                GrassDitherClip(input.positionCS.xy, input.fade);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
}
