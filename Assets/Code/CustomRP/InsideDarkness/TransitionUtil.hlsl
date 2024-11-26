#include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"

#include_with_pragmas "Assets/Code/CustomRP/Shaders/BasicShaders/Lit/LitUtilMaterial.hlsl"

cbuffer UnityPerMaterial
{
	float3 _TransitionScale;
};

#include_with_pragmas "Assets/Code/CustomRP/Shaders/BasicShaders/Lit/LitUtilAttribs.hlsl"

struct Varyings
{
	float4 positionCS 	: SV_POSITION;
	float3 positionWS 	: POSITIONT;
	float ambientScale 	: TEXCOORD1;

	#if _TRIPLANAR_ON
		int3 mask : TEXCOORD0;
	#else
		float2 uv : TEXCOORD0;
	#endif

	#ifdef VARYING_NORMAL
	float3 normalWS		: NORMAL;
	#endif
};

#include_with_pragmas "Assets/Code/CustomRP/Shaders/BasicShaders/Lit/LitUtilTriplanar.hlsl"

#pragma vertex vert
#pragma fragment frag

Varyings vert(Attributes IN)
{
	Varyings OUT;

	float3 worldPos = ObjToWorld(IN.positionOS);

	#if defined(VARYING_NORMAL) || _TRIPLANAR_ON
		float3 worldNorm = ObjToWorld_Direction(IN.normalOS);
	#endif

	#ifdef VARYING_NORMAL
		OUT.normalWS = worldNorm;
	#endif

	OUT.positionCS = WorldToHClip(worldPos);
	OUT.positionWS = worldPos;

	#if _TRIPLANAR_ON
		OUT.mask = GetMask(worldNorm);
	#else
		OUT.uv = IN.uv;
	#endif

	OUT.ambientScale = saturate(dot(IN.positionOS.xyz, _TransitionScale));

	return OUT;
}