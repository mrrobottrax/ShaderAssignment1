// This defines a simple unlit Shader object that is compatible with a custom Scriptable Render Pipeline.
// It applies a hardcoded color, and demonstrates the use of the LightMode Pass tag.
// It is not compatible with SRP Batcher.

Shader "BasicLit"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			// The value of the LightMode Pass tag must match the ShaderTagId in ScriptableRenderContext.DrawRenderers
			Tags { "LightMode" = "FORWARDBASE"}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4x4 unity_MatrixVP;
			float4x4 unity_ObjectToWorld;

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
				float4 color = tex2D(_MainTex, IN.uvs * _MainTex_ST.xy + _MainTex_ST.zw).rgba;
				return color;
			}
			ENDHLSL
		}
	}
}