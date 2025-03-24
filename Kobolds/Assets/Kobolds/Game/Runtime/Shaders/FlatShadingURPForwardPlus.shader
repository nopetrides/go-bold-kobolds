Shader "Custom/FlatShadingURP_ForwardPlus"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _FORWARD_PLUS // Enables Forward+ compatibility
            #pragma once

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                // to support GPU instancing and Single Pass Stereo rendering(VR), add the following section
                UNITY_VERTEX_INPUT_INSTANCE_ID  // For non PSSL, equals to -> uint instanceID : SV_InstanceID;
                UNITY_VERTEX_OUTPUT_STEREO      // For non OpenGL and non PSSL, equals to -> uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex; (when UNITY_STEREO_INSTANCING_ENABLED)
            };


            // all sampler2D don't need to put inside CBUFFER 
            sampler2D _BaseMap; 
            sampler2D _EmissionMap;
            sampler2D _OcclusionMap;
            sampler2D _OutlineZOffsetMaskTex;

            // put all your uniforms(usually things inside .shader file's properties{}) inside this CBUFFER, in order to make SRP batcher compatible
            // see -> https://blogs.unity3d.com/2019/02/28/srp-batcher-speed-up-your-rendering/
            CBUFFER_START(UnityPerMaterial)
                // base color
                float4  _BaseMap_ST;
                half4   _BaseColor;

                // alpha
                half    _Cutoff;

                // emission
                float   _UseEmission;
                half3   _EmissionColor;
                half    _EmissionMulByBaseColor;
                half3   _EmissionMapChannelMask;

                // occlusion
                float   _UseOcclusion;
                half    _OcclusionStrength;
                half4   _OcclusionMapChannelMask;
                half    _OcclusionRemapStart;
                half    _OcclusionRemapEnd;

                // lighting
                half3   _IndirectLightMinColor;
                half    _CelShadeMidPoint;
                half    _CelShadeSoftness;

                // shadow mapping
                half    _ReceiveShadowMappingAmount;
                float   _ReceiveShadowMappingPosOffset;
                half3   _ShadowMapColor;

                // outline
                float   _OutlineWidth;
                half3   _OutlineColor;
                float   _OutlineZOffset;
                float   _OutlineZOffsetMaskRemapStart;
                float   _OutlineZOffsetMaskRemapEnd;

            CBUFFER_END

            //a special uniform for applyShadowBiasFixToHClipPos() only, it is not a per material uniform, 
            //so it is fine to write it outside our UnityPerMaterial CBUFFER
            float3 _LightDirection;
            
            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // Apply shading per-light
            float3 ShadeSingleLight(Light light, float3 normal, float3 positionWS)
            {
                float3 lightDir = normalize(light.direction);
                half distanceAttenuation = min(4,light.distanceAttenuation);
                half litOrShadowArea = lerp(1,light.shadowAttenuation,_ReceiveShadowMappingAmount);
                half3 litOrShadowColor = lerp(_ShadowMapColor,1, litOrShadowArea);
                half3 lightAttenuationRGB = litOrShadowColor * distanceAttenuation;
                
                // If it's a point or spot light, calculate correct direction
                if (light.distanceAttenuation < 1.0)
                {
                    lightDir = normalize(light.direction - positionWS);
                }

                // Diffuse Lambert shading
                float diff = max(dot(normal, lightDir), 0.0);
                
                return saturate(light.color) * diff * distanceAttenuation; //(litOrShadowColor ? 0.25 : 1);
                
                //
                // half3 N = normal;
                // half3 L = light.direction;
                //
                // half NoL = dot(N,L);
                //
                // half lightAttenuation = 1;
                //
                // // light's distance & angle fade for point light & spot light (see GetAdditionalPerObjectLight(...) in Lighting.hlsl)
                // // Lighting.hlsl -> https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl
                // half distanceAttenuation = min(4,light.distanceAttenuation); //clamp to prevent light over bright if point/spot light too close to vertex
                //
                // // N dot L
                // // simplest 1 line cel shade, you can always replace this line by your own method!
                // half litOrShadowArea = smoothstep(_CelShadeMidPoint-_CelShadeSoftness,_CelShadeMidPoint+_CelShadeSoftness, half3(0,0,0));
                //
                // // occlusion
                // litOrShadowArea *= _OcclusionStrength;
                //
                // // face ignore celshade since it is usually very ugly using NoL method
                // litOrShadowArea = _IsFace? lerp(0.5,1,litOrShadowArea) : litOrShadowArea;
                //
                // // light's shadow map
                // litOrShadowArea *= lerp(1,light.shadowAttenuation,_ReceiveShadowMappingAmount);
                //
                // half3 litOrShadowColor = lerp(_ShadowMapColor,1, litOrShadowArea);
                //
                // half3 lightAttenuationRGB = litOrShadowColor * distanceAttenuation;
                //
                // // saturate() light.color to prevent over bright
                // // additional light reduce intensity since it is additive
                // return saturate(light.color) * lightAttenuationRGB * (true ? 0.25 : 1);


                //
                // float3 lightDir = normalize(light.direction);
                //
                // // If it's a point or spot light, calculate correct direction
                // if (light.distanceAttenuation < 1.0)
                // {
                //     lightDir = normalize(light.direction - positionWS);
                // }
                //
                // // Diffuse Lambert shading
                // float diff = max(dot(normal, lightDir), 0.0);
                //
                // // Return color contribution
                // return light.color * diff * light.distanceAttenuation * light.shadowAttenuation;
            }

            half3 CompositeAllLightResults(half3 indirectResult, half3 mainLightResult, half3 additionalLightSumResult, half3 emissionResult, half3 albedo)
            {
                // [remember you can write anything here, this is just a simple tutorial method]
                // here we prevent light over bright,
                // while still want to preserve light color's hue
                half3 rawLightSum = max(indirectResult, mainLightResult + additionalLightSumResult); // pick the highest between indirect and direct light
                return albedo * rawLightSum + emissionResult;
            }

            // Fragment Shader
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);                     // in non OpenGL and non PSSL, MACRO will turn into -> UnitySetupInstanceID(input.instanceID);
                
                float3 normal = normalize(IN.normalWS);
                float3 lighting = 0;

                // Create InputData struct to pass to GetAdditionalLight
                InputData inputData;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normal;
                inputData.positionCS = IN.positionCS;
                inputData.viewDirectionWS = float3(0.0, 0.0, 1.0); // Default value for view direction
                inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0); // Default value for shadow coordinates
                inputData.fogCoord = 1.0; // Default fog value
                inputData.vertexLighting = half3(0.0, 0.0, 0.0); // Default lighting
                inputData.bakedGI = half3(0.0, 0.0, 0.0); // Default baked GI
                inputData.normalizedScreenSpaceUV = float2(0.0, 0.0); // Default UV
                inputData.shadowMask = half4(0.0, 0.0, 0.0, 0.0); // Default shadow mask
                inputData.tangentToWorld = half3x3(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0); // Default tangent-to-world matrix

                // Check and loop over additional lights for Forward+ mode
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_FORWARD_PLUS
                        // Loop through directional lights
                        for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                        {
                            // Check if the light is subtractive
                            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

                            Light light = GetAdditionalLight(lightIndex, IN.positionWS);

                            #ifdef _LIGHT_LAYERS
                                if (IsMatchingLightLayer(light.layerMask, 0)) // Assuming we check against the default mesh layer
                                    lighting += ShadeSingleLight(light, normal, IN.positionWS);
                            #else
                                lighting += ShadeSingleLight(light, normal, IN.positionWS);
                            #endif
                        }
                    #endif

                    // Loop through all pixel lights
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, IN.positionWS);

                        #ifdef _LIGHT_LAYERS
                            if (IsMatchingLightLayer(light.layerMask, 0)) // Assuming we check against the default mesh layer
                                lighting += ShadeSingleLight(light, normal, IN.positionWS);
                        #else
                            lighting += ShadeSingleLight(light, normal, IN.positionWS);
                        #endif
                    LIGHT_LOOP_END
                #endif

                // Final color
                tex2D(_BaseMap, IN.positionWS) * _BaseColor;
                float3 finalColor = CompositeAllLightResults (lighting, lighting, lighting, lighting, _BaseColor.rgb);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
