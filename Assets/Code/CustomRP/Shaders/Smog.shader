Shader "CustomRP/Smog"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType"="Transparent"
			"IgnoreProjector"="True"
		}

		Pass
		{
			Tags
			{ 
				"LightMode" = "ForwardBase"
			}

			ZWrite Off
			Blend DstColor SrcColor

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"

			cbuffer UnityPerMaterial
			{
				sampler2D _MainTex;
				float4 _MainTex_ST;
			};

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float2 uvs			: TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS 	: SV_POSITION;
				float2 uvs			: TEXCOORD0;
			};

			Varyings vert (Attributes IN)
			{
				Varyings OUT;
				float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);
				OUT.positionCS = mul(unity_MatrixVP, worldPos);
				OUT.uvs = IN.uvs;
				return OUT;
			}

			float4 frag (Varyings IN) : SV_TARGET
			{
				float3 color = 0.5 - tex2D(_MainTex, IN.uvs * _MainTex_ST.xy + _MainTex_ST.zw).rgb;
				return float4(color.rgb, 1);
			}
			ENDHLSL
		}
	}
}