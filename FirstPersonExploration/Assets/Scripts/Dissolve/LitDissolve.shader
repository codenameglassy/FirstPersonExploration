// LitDissolve.shader
// URP lit shader with two dissolve modes.
// Clip: pixels below the noise threshold are discarded (object disappears).
// Swap: statue surface dissolves into the original surface in one draw call.
// Works on MeshRenderer and SkinnedMeshRenderer. Noise is sampled in UV space so it sticks to animated surfaces.
// Shadow, depth and depth-normals passes clip too, so shadows and SSAO match the visible shape.
Shader "Custom/Lit Dissolve"
{
    Properties
    {
        [Header(Original Surface)]
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Header(Statue Surface for Swap Mode)]
        _StatueMap ("Statue Base Map", 2D) = "white" {}
        _StatueColor ("Statue Color", Color) = (0.6, 0.6, 0.6, 1)
        [Normal] _StatueBumpMap ("Statue Normal Map", 2D) = "bump" {}
        _StatueBumpScale ("Statue Normal Scale", Float) = 1.0
        _StatueMetallic ("Statue Metallic", Range(0, 1)) = 0.0
        _StatueSmoothness ("Statue Smoothness", Range(0, 1)) = 0.2

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
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Metallic;
            half _Smoothness;
            float4 _StatueMap_ST;
            half4 _StatueColor;
            half _StatueBumpScale;
            half _StatueMetallic;
            half _StatueSmoothness;
            float4 _DissolveNoise_ST;
            half _DissolveAmount;
            half _EdgeWidth;
            half4 _EdgeColor;
        CBUFFER_END

        TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_StatueMap);      SAMPLER(sampler_StatueMap);
        TEXTURE2D(_StatueBumpMap);  SAMPLER(sampler_StatueBumpMap);
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
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _DISSOLVEMODE_CLIP _DISSOLVEMODE_SWAP

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float2 uv                       : TEXCOORD0;
                float3 positionWS               : TEXCOORD1;
                float3 normalWS                 : TEXCOORD2;
                float4 tangentWS                : TEXCOORD3;
                half4 fogFactorAndVertexLight   : TEXCOORD4;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord              : TEXCOORD5;
            #endif
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.normalWS = normalInputs.normalWS;

                float tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = float4(normalInputs.tangentWS, tangentSign);

                half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
                half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(positionInputs);
            #endif

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half noise = SampleDissolveNoise(input.uv);
                float cutoff = GetDissolveCutoff();
                half edge = GetDissolveEdge(noise, cutoff);

                float2 baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV).rgb * _BaseColor.rgb;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, baseUV), _BumpScale);
                half metallic = _Metallic;
                half smoothness = _Smoothness;

            #if defined(_DISSOLVEMODE_CLIP)
                clip(noise - cutoff);
            #elif defined(_DISSOLVEMODE_SWAP)
                // Statue stays where noise is at or above the cutoff; the original surface is revealed below it.
                half statueMask = step(cutoff, noise);

                float2 statueUV = TRANSFORM_TEX(input.uv, _StatueMap);
                half3 statueAlbedo = SAMPLE_TEXTURE2D(_StatueMap, sampler_StatueMap, statueUV).rgb * _StatueColor.rgb;
                half3 statueNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_StatueBumpMap, sampler_StatueBumpMap, statueUV), _StatueBumpScale);

                albedo = lerp(albedo, statueAlbedo, statueMask);
                normalTS = lerp(normalTS, statueNormalTS, statueMask);
                metallic = lerp(metallic, _StatueMetallic, statueMask);
                smoothness = lerp(smoothness, _StatueSmoothness, statueMask);

                // Edge glow only burns on the statue side of the boundary.
                edge *= statueMask;
            #endif

                float3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                float3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
                inputData.fogCoord = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = unity_ProbesOcclusion;
                inputData.tangentToWorld = tangentToWorld;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = _EdgeColor.rgb * edge;
                surfaceData.alpha = 1.0h;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0h;
                return color;
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
