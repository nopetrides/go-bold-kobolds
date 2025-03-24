Shader "Custom/FlatShadingURP"
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
            #pragma once
            
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Include this if you are doing a lit shader. This includes lighting shader variables,
            // lighting and shadow functions
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
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END

            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // Shade a single light (Directional, Point, or Spot)
            float3 ShadeSingleLight(Light light, float3 normal)
            {
                float3 lightDir = normalize(light.direction);

                // If this is a point/spot light, reconstruct the direction
                if (light.distanceAttenuation < 1.0)
                {
                    lightDir = normalize(-light.direction); // Approximate light direction
                }

                // Debug: Check if light exists (Make material color the light color)
                if (light.distanceAttenuation <= 0.01)
                {
                    return float3(1, 0, 1); // Bright pink (Error color)
                }

                // Diffuse shading (Lambert)
                float diff = max(dot(normal, lightDir), 0.0);

                // If no light is affecting, force visibility
                if (diff < 0.01)
                {
                    return float3(0.1, 0.1, 0.1); // Faint ambient light
                }

                return light.color * diff * light.distanceAttenuation * light.shadowAttenuation;
            }

            // Fragment Shader
            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 lighting = 0;

                // Main Directional Light
                Light mainLight = GetMainLight();
                lighting += ShadeSingleLight(mainLight, normal);

                // Additional Lights (Point & Spot)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    lighting += ShadeSingleLight(light, normal);
                }

                // If lighting is too dark, apply fallback brightness
                if (dot(lighting, float3(1, 1, 1)) < 0.01)
                {
                    lighting += float3(0.1, 0.1, 0.1); // Ensure visibility
                }

                // Final color
                float3 finalColor = _BaseColor.rgb * lighting;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
