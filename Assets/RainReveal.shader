Shader "MajorProject/RainReveal"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _RevealCenter ("Reveal Center", Vector) = (0, 0, 0, 0)
        _RevealRadius ("Reveal Radius", Float) = 3
        _RevealSoftness ("Reveal Softness", Float) = 1
        _RevealEnabled ("Reveal Enabled", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 worldXY : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _RevealCenter;
                float _RevealRadius;
                float _RevealSoftness;
                float _RevealEnabled;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(worldPos);
                output.color = input.color * _BaseColor;
                output.worldXY = worldPos.xy;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = input.color;

                if (_RevealEnabled > 0.5)
                {
                    float softness = max(_RevealSoftness, 0.001);
                    float distanceToLight = distance(input.worldXY, _RevealCenter.xy);
                    float reveal = 1.0 - smoothstep(_RevealRadius - softness, _RevealRadius + softness, distanceToLight);
                    color.a *= saturate(reveal);
                }

                return color;
            }
            ENDHLSL
        }
    }
}
