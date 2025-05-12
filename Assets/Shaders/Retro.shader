Shader "Custom/Retro"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}
        _PixelSize("Pixel Size", Range(1, 64)) = 4
        _ColorSteps("Color Steps", Range(1, 256)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 positionHCS : SV_POSITION;
                float3 worldUV : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            float4 _BaseColor;
            float _PixelSize;
            float _ColorSteps;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldUV = OUT.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
                return OUT;
            }           

            half4 frag(Varyings IN) : SV_Target
            {
                // Calculate pixelation
                float2 screenPos = IN.positionHCS.xy / _ScreenParams.xy;
                float2 pixelatedUV = screenPos;

                float2 pixelatedScreenPos = floor(IN.positionHCS.xy / _PixelSize) * _PixelSize;
                pixelatedUV = pixelatedScreenPos / _ScreenParams.xy;

                // Sample texture with pixelation
                half4 texColor = tex2D(_MainTex, pixelatedUV);
                float3 finalColor = texColor.rgb * _BaseColor.rgb;

                // Quantization effect
                float3 quantizedColor = floor(finalColor * _ColorSteps) / _ColorSteps;

                return half4(quantizedColor, 1);
            }
            ENDHLSL
        }
    }
}