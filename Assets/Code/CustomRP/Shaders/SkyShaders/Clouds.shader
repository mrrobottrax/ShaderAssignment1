Shader "CustomRP/Clouds"
{
	Properties
	{
		[Header(Cloud Properties)]
        _CloudTex("Cloud Texture", 2D) = "black" {}
        _CloudColor("Cloud Color", Color) = (1,1,1,1)
        _CloudCutoff("Clouds Cutoff", Range(0, 1)) = 0.08
        _CloudSpeedX("Clouds Move Speed X", Range(0, 1)) = 0.3 
        _CloudSpeedY("Clouds Move Speed Y", Range(0, 1)) = 0.3 
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

			
            sampler2D _CloudTex;
			float4 _CloudColor;

            // Scale
			float4 _CloudTex_ST;

            // Properties
            float _CloudCutoff;
            float _CloudSpeedX;
            float _CloudSpeedY;

            // Time
            float4 _Time;

			Varyings vert (Appdata IN)
			{
				Varyings OUT;

				float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);
				OUT.positionCS = mul(unity_MatrixVP, worldPos);

				OUT.uv = IN.uv;
                OUT.uv.x += _CloudSpeedX * sin(_Time.x);
                OUT.uv.y += _CloudSpeedY * sin(_Time.x);

				return OUT;
			}

			float4 frag (Varyings IN) : SV_TARGET
			{
				float4 color = tex2D(_CloudTex, IN.uv * _CloudTex_ST.xy + _CloudTex_ST.zw) * _CloudColor;
				return color;
			}
			ENDHLSL
		}
	}
}