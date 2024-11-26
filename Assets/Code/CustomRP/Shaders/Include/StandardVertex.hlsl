#pragma vertex vert

#include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"

cbuffer UnityPerMaterial
{
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	
	float4 _Color;
};

#include_with_pragmas "Assets/Code/CustomRP/Shaders/Include/Triplanar.hlsl"

struct Attributes
{
	float3 positionOS   : POSITION;

	#if _TRIPLANAR_ON
		float3 normalOS   	: NORMAL;
	#else
		float2 uv			: TEXCOORD0;
	#endif
};

struct Varyings
{
	float4 positionCS 	: SV_POSITION;
	
	#if _TRIPLANAR_ON
		float3 positionWS : POSITIONT;
		int3 mask : TEXCOORD0;
	#else
		float2 uv : TEXCOORD0;
	#endif
};

Varyings vert(Attributes IN)
{
	Varyings OUT;

	float3 worldPos = ObjToWorld(IN.positionOS);
	OUT.positionCS = WorldToHClip(worldPos);

	#if _TRIPLANAR_ON
		float3 worldNorm = ObjToWorld_Direction(IN.normalOS);
		OUT.mask = GetMask(worldNorm);

		OUT.positionWS = worldPos;
	#else
		OUT.uv = IN.uv;
	#endif

	return OUT;
}