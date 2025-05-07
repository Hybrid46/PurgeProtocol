Shader "Custom/Light"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _NoiseScale("Noise Scale (XYZ scale, W speed)", Vector) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 5)) = 1.0
        _PixelSize("Pixel Size", Range(1, 64)) = 16
        _ColorSteps("Color Steps", Range(1, 8)) = 4
        _Turbulence("Turbulence", Range(0, 1)) = 0.2
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
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float4 positionHCS : SV_POSITION;
                float3 worldUV : TEXCOORD3;
            };

            float4 _NoiseScale;
            float4 _BaseColor;
            float _FresnelPower;
            float _FresnelIntensity;
            float _PixelSize;
            float _ColorSteps;
            float _Turbulence;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                OUT.worldUV = OUT.positionWS * _NoiseScale.xyz;
                return OUT;
            }

            //noise functions
            inline float unity_noise_randomValue (float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
            }

            inline float unity_noise_interpolate (float a, float b, float t)
            {
                return (1.0-t)*a + (t*b);
            }

            inline float unity_valueNoise (float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                uv = abs(frac(uv) - 0.5);
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0 = unity_noise_randomValue(c0);
                float r1 = unity_noise_randomValue(c1);
                float r2 = unity_noise_randomValue(c2);
                float r3 = unity_noise_randomValue(c3);

                float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
                float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
                float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
                return t;
            }

            void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
            {
                float t = 0.0;

                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3-0));
                t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3-1));
                t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3-2));
                t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                Out = t;
            }

            void MultiLayerNoise_float(float3 WorldUV, float Scale, float Speed, out float Out)
            {
                // Base noise layer with rotating UVs
                float2 baseUV = WorldUV.xy;
                float rotationAngle = _Time.y * 0.5;
                float2x2 rotMatrix = float2x2(
                    cos(rotationAngle), -sin(rotationAngle),
                    sin(rotationAngle), cos(rotationAngle)
                );
                baseUV = mul(rotMatrix, baseUV);
    
                // distortion calculation
                float distortionX, distortionY;
                Unity_SimpleNoise_float(baseUV * 2.0 + _Time.y * 0.7, Scale * 1.5, distortionX);
                Unity_SimpleNoise_float(baseUV * 2.0 + _Time.y * 1.3, Scale * 1.5, distortionY); // Different time offset for Y
                float2 distortion = (float2(distortionX, distortionY) - 0.5) * 2.0 * _Turbulence;

                // Combine multiple noise layers
                float noise1, noise2, noise3;
    
                // Layer 1: Rotating base noise with distortion
                Unity_SimpleNoise_float(baseUV * Scale + distortion + float2(_Time.y * Speed, 0), Scale, noise1);
    
                // Layer 2: Perpendicular movement
                Unity_SimpleNoise_float(baseUV * Scale * 0.8 + distortion.yx + float2(0, _Time.y * Speed * 0.7), Scale * 0.8, noise2);
    
                // Layer 3: Inverse movement
                Unity_SimpleNoise_float(baseUV * Scale * 1.2 - distortion - float2(_Time.y * Speed * 0.5, _Time.y * Speed * 0.3), Scale * 1.2, noise3);

                Out = (noise1 * 0.6 + noise2 * 0.3 + noise3 * 0.1);
            }
            //-------------

            half4 frag(Varyings IN) : SV_Target
            {
                                float noise;
                MultiLayerNoise_float(IN.worldUV, _NoiseScale.x, _NoiseScale.w, noise);

                // Add Z-axis variation
                noise += sin(IN.worldUV.z * _NoiseScale.z + _Time.y * 0.5) * 0.1;

                // Fresnel effect
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDir)), _FresnelPower) * _FresnelIntensity;
                float fresnelNoise = noise * (1.0 + fresnel);

                // Lighting calculations
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 directLight = LightingLambert(mainLight.color, mainLight.direction, normalWS);
                float3 totalDiffuse = directLight;

                #ifdef _ADDITIONAL_LIGHTS
                uint numAdditionalLights = GetAdditionalLightsCount();
                for (uint i = 0; i < numAdditionalLights; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    totalDiffuse += LightingLambert(light.color, light.direction, normalWS);
                }
                #endif

                // Original color
                float3 finalColor = _BaseColor.rgb * fresnelNoise * totalDiffuse;

                // Pixelation effect
                float2 pixelatedScreenPos = floor(IN.positionHCS.xy / _PixelSize) * _PixelSize;
                float randomSeed = dot(pixelatedScreenPos, float2(12.9898, 78.233));
                
                // Quantize color
                float3 quantizedColor = floor(finalColor * _ColorSteps) / _ColorSteps;
                
                // Add pixel grid effect
                float2 pixelBorder = saturate(frac(IN.positionHCS.xy / _PixelSize) * 2.0);
                float borderMask = 1.0 - step(0.9, max(pixelBorder.x, pixelBorder.y));
                
                return half4(quantizedColor * borderMask, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}