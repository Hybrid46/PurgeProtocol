Shader "Custom/Light"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _NoiseTexture("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float4 positionHCS : SV_POSITION;
            };

            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);
            float4 _NoiseScale;
            float4 _BaseColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv * _NoiseScale.xy;
                
                // Shadow coordinate for the main light
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample noise texture
                float3 noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, IN.uv);

                // Normalize world-space normal
                float3 normalWS = normalize(IN.normalWS);

                // Get main light with shadow attenuation
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 directLight = LightingLambert(mainLight.color, mainLight.direction, normalWS);
                float3 totalDiffuse = directLight;

                // Additional lights loop
                #ifdef _ADDITIONAL_LIGHTS
                uint numAdditionalLights = GetAdditionalLightsCount();
                for (uint i = 0; i < numAdditionalLights; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    totalDiffuse += LightingLambert(light.color, light.direction, normalWS);
                }
                #endif

                // Combine base color and noise
                float3 finalColor = _BaseColor.rgb * noise * totalDiffuse;
                
                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // Shadow-caster pass (required for shadows)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}