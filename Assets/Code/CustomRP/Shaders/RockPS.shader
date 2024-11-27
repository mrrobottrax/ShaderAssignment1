Shader "CustomRP/RockPS"
{
    Properties
    {
        _DissolveTex ("Dissolve Texture", 2D) = "white" {} 
        _Threshold ("Dissolve Threshold", Range(0, 1)) = 0.5
        _BaseColour ("Colour", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags
            { 
                "LightMode" = "ForwardBase"
            }

            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"
            
            struct Appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Texture2D _DissolveTex;
            SamplerState sampler_DissolveTex;
            float _Threshold;
            float4 _BaseColour;
            
            float4 _Time;

            Varyings vert (Appdata IN)
            {
                Varyings OUT;

                float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);
                OUT.positionCS = mul(unity_MatrixVP, worldPos);
                OUT.uv = IN.uv;

                return OUT;
            }
            
            float4 frag (Varyings IN) : SV_TARGET
            {
                float dissolveValue = _DissolveTex.Sample(sampler_DissolveTex, IN.uv).r;
                
                if (dissolveValue < _Threshold)
                {
                    clip(-1);
                }

                return _BaseColour;
            }
            ENDHLSL
        }
        UsePass "CustomRP/Lit/ShadowCasting"
    }
}
