Shader "CustomRP/Lit"
{
	Properties
	{
		[Toggle] _TRIPLANAR ("Triplanar", Float) = 1
		[Toggle] _ROTATE_90 ("Rotate Triplanar", Float) = 1
		[Toggle] _SMOOTH_SHADING ("Smooth Shading", Float) = 1
		[Toggle] _DISABLE_DIRECTIONAL ("Disable Directional Shadow Casting", Float) = 0
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_AmbientScale ("Ambient Intensity", Range(0, 1)) = 1
	}

	SubShader
	{
		Tags
		{
			"TerrainCompatible" = "True"
		}

		Pass
		{
			Name "BaseColor"

			Tags
			{ 
				"LightMode" = "Deferred"
			}

			ZTest LEqual

			HLSLPROGRAM

			#pragma shader_feature_local _SMOOTH_SHADING_ON

			#pragma vertex vert
			#pragma fragment frag

			#include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"
			#include_with_pragmas "Assets/Code/CustomRP/Shaders/Include/Triplanar.hlsl"

			cbuffer UnityPerMaterial
			{
				float4 _Color;
				float _AmbientScale;
				
				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;
			};

			struct Attributes
			{
				float3 positionOS   : POSITION;
				float3 normalOS   	: NORMAL;

				#if !_TRIPLANAR_ON
				float2 uv			: TEXCOORD0;
				#endif
			};

			struct Varyings
			{
				float4 positionCS 	: SV_POSITION;
				float3 positionWS 	: POSITIONT;

				#if !_SMOOTH_SHADING_ON
				nointerpolation
				#endif
				float3 normalWS		: NORMAL;

				#if _TRIPLANAR_ON
					int3 mask : TEXCOORD0;
				#else
					float2 uv : TEXCOORD0;
				#endif
			};

			struct GBuffer
			{
				float4 albedo 	: SV_TARGET0;
				float3 normal 	: SV_TARGET1;
				float3 position : SV_TARGET2;
			};

			float4 Sample(Varyings IN)
			{
				#if _TRIPLANAR_ON
					return _Color * SampleTriplanar(_MainTex, _MainTex_ST, _MainTex_TexelSize, IN.positionWS, IN.mask);
				#else
					return _Color * tex2D(_MainTex, IN.uv * _MainTex_ST.xy + _MainTex_ST.zw);
				#endif
			}	
			
			Varyings vert(Attributes IN)
			{
				Varyings OUT;

				float3 worldPos = ObjToWorld(IN.positionOS);
				float3 worldNorm = ObjToWorld_Direction(IN.normalOS);

				OUT.positionCS = WorldToHClip(worldPos);
				OUT.positionWS = worldPos;

				OUT.normalWS = worldNorm;

				#if _TRIPLANAR_ON
					OUT.mask = GetMask(worldNorm);
				#else	
					OUT.uv = IN.uv;
				#endif	

				return OUT;
			}	

			GBuffer frag(Varyings IN)
			{
				GBuffer OUT;

				float4 color = Sample(IN);

				OUT.albedo = float4(color.rgb, _AmbientScale);
				OUT.normal = IN.normalWS;
				OUT.position = IN.positionWS;

				if (color.a < 0.5) discard;
				return OUT;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCasting"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			Cull Off
			ZTest LEqual
			ZWrite Off
			Blend Zero One
			Offset 0.009, 0

			Stencil
			{
				Comp Always

				ZFailBack IncrWrap
				ZFailFront DecrWrap
			}

			HLSLPROGRAM

			#pragma shader_feature_local _DISABLE_DIRECTIONAL_ON
			
			// Optimize out shadow casting for mines
			#if _DISABLE_DIRECTIONAL_ON
			#define DISABLE_DIRECTIONAL
			#endif
			#include_with_pragmas "Assets/Code/CustomRP/Shaders/Include/ShadowUtil.hlsl"

			ENDHLSL
		}
	}
}