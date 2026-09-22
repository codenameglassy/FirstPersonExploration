// UnlitDissolve.shader
// Flat unlit URP shader with two dissolve modes. No lighting: output is texture x color, plus fog and edge glow.
// Clip: pixels below the noise threshold are discarded (object disappears).
// Swap: statue surface dissolves into the original surface in one draw call.
// Works on MeshRenderer and SkinnedMeshRenderer. Noise is sampled in UV space so it sticks to animated surfaces.
// Still casts shadows, and the shadow, depth and depth-normals passes clip in Clip mode so they match the visible shape.
Shader "Custom/Unlit Dissolve"
{
    Properties
    {
        [Header(Original Surface)]
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Statue Surface for Swap Mode)]
        _StatueMap ("Statue Base Map", 2D) = "white" {}
        _StatueColor ("Statue Color", Color) = (0.6, 0.6, 0.6, 1)

        [Header(Dissolve)]
        [KeywordEnum(Clip, Swap)] _DissolveMode ("Dissolve Mode", Float) = 0
        _DissolveNoise ("Noise (R)", 2D) = "gray" {}
        _DissolveAmount ("Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.05
        [HDR] _EdgeColor ("Edge Color", Color) = (4, 1.5, 0.5, 1)

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float4 _StatueMap_ST;
            half4 _StatueColor;
            float4 _DissolveNoise_ST;
            half _DissolveAmount;
            half _EdgeWidth;
            half4 _EdgeColor;
        CBUFFER_END

        TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_StatueMap);      SAMPLER(sampler_StatueMap);
        TEXTURE2D(_DissolveNoise);  SAMPLER(sampler_DissolveNoise);

        static const float DISSOLVE_EPSILON = 0.001;

        // Remaps Amount so 0 is fully intact with the edge band hidden, and 1 is fully dissolved.
        float GetDissolveCutoff()
        {
            float width = max(_EdgeWidth, DISSOLVE_EPSILON);
            return lerp(-width - DISSOLVE_EPSILON, 1.0 + DISSOLVE_EPSILON, _DissolveAmount);
        }

        half SampleDissolveNoise(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_DissolveNoise, sampler_DissolveNoise, TRANSFORM_TEX(uv, _DissolveNoise)).r;
        }

        // 1 at the cutoff, fading to 0 at cutoff + edge width.
        half GetDissolveEdge(half noise, float cutoff)
        {
            float width = max(_EdgeWidth, DISSOLVE_EPSILON);
            return 1.0h - (half)saturate((noise - cutoff) / width);
        }

        void ApplyDissolveClip(float2 uv)
        {
        #if defined(_DISSOLVEMODE_CLIP)
            clip(SampleDissolveNoise(uv) - GetDissolveCutoff());
        #endif
        }
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _DISSOLVEMODE_CLIP _DISSOLVEMODE_SWAP
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half fogFactor    : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half noise = SampleDissolveNoise(input.uv);
                float cutoff = GetDissolveCutoff();
                half edge = GetDissolveEdge(noise, cutoff);

                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(input.uv, _BaseMap)).rgb * _BaseColor.rgb;

            #if defined(_DISSOLVEMODE_CLIP)
                clip(noise - cutoff);
            #elif defined(_DISSOLVEMODE_SWAP)
                // Statue stays where noise is at or above the cutoff; the original surface is revealed below it.
                half statueMask = step(cutoff, noise);
                half3 statueColor = SAMPLE_TEXTURE2D(_StatueMap, sampler_StatueMap, TRANSFORM_TEX(input.uv, _StatueMap)).rgb * _StatueColor.rgb;
                color = lerp(color, statueColor, statueMask);

                // Edge glow only burns on the statue side of the boundary.
                edge *= statueMask;
            #endif

                color += _EdgeColor.rgb * edge;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local _DISSOLVEMODE_CLIP _DISSOLVEMODE_SWAP
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // Shadows.hlsl uses LerpWhiteTo from CommonMaterial.hlsl but does not include it itself.
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                ApplyDissolveClip(input.uv);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #pragma shader_feature_local _DISSOLVEMODE_CLIP _DISSOLVEMODE_SWAP

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                ApplyDissolveClip(input.uv);
                return half4(input.positionCS.z, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #pragma shader_feature_local _DISSOLVEMODE_CLIP _DISSOLVEMODE_SWAP

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                ApplyDissolveClip(input.uv);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
