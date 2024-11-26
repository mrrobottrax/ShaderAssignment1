#include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"

#pragma vertex vert
#pragma fragment frag
#pragma geometry geom

#pragma multi_compile DIRECTIONAL _

float4 _WorldSpaceLightPos0;
float _LightRange0;

struct ATTRIB
{
	float3 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
};

struct V2G
{
	float3 positionWS	: POSITIONT;
};

struct G2F
{
	float4 positionCS	: SV_POSITION;
};

V2G vert(ATTRIB IN)
{
	V2G OUT;
	float3 worldPos = ObjToWorld(IN.positionOS);

	OUT.positionWS = worldPos;
	return OUT;
}

void DirectionalShadows(V2G input[3], float3 normal, inout TriangleStream<G2F> triStream)
{
	// Check if the face is in shadow
	float d = dot(normal, _WorldSpaceLightPos0.xyz);

	// Face in shadow
	if (d > 0) return;

	float3 worldPos;
	G2F output;

	// Reverse faces for front cap
	float3 centre = 0;
	for (int i = 2; i >= 0; --i)
	{
		worldPos = input[i].positionWS;
		centre += worldPos.xyz / 3;
		output.positionCS = WorldToHClip(worldPos);

		triStream.Append(output);
	}

	float3 offset = _WorldSpaceLightPos0.xyz * -1024;

	// Expand faces outwards by a bit so there are no gaps
	float expansion = 2;
	float3 backCap[3];
	for (int j = 0; j < 3; ++j)
	{
		float3 pos = (input[j].positionWS.xyz - centre) * expansion + centre;
		pos += offset;
		backCap[j] = pos;
	}

	worldPos = backCap[0];
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = input[2].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = backCap[2];
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = backCap[1];
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = backCap[0];
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = input[1].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	triStream.RestartStrip();

	worldPos = input[1].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = input[2].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = backCap[1];
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);
}

void PointShadows(V2G input[3], float3 normal, inout TriangleStream<G2F> triStream)
{
	// Check if the face is in shadow
	float d = dot(normal, _WorldSpaceLightPos0.xyz - input[0].positionWS.xyz);

	// Face in shadow
	if (d > 0) return;

	float3 worldPos;
	G2F output;

	// Reverse faces for front cap
	for (int i = 2; i >= 0; --i)
	{
		worldPos = input[i].positionWS;
		output.positionCS = WorldToHClip(worldPos);

		triStream.Append(output);
	}

	float3 offset0 = input[0].positionWS + normalize(input[0].positionWS - _WorldSpaceLightPos0.xyz) * 1000;
	float3 offset1 = input[1].positionWS + normalize(input[1].positionWS - _WorldSpaceLightPos0.xyz) * 1000;
	float3 offset2 = input[2].positionWS + normalize(input[2].positionWS - _WorldSpaceLightPos0.xyz) * 1000;

	//triStream.RestartStrip();

	worldPos = offset0;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = input[2].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = offset2;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = offset1;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = offset0;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = input[1].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	triStream.RestartStrip();

	worldPos = input[1].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = input[2].positionWS;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);

	worldPos = offset1;
	output.positionCS = WorldToHClip(worldPos);
	triStream.Append(output);
}

[maxvertexcount(12)]
void geom(triangle V2G input[3], inout TriangleStream<G2F> triStream, uint id : SV_PrimitiveID)
{
	// Calculate the normal for the main triangle
	float3 v0 = input[0].positionWS.xyz;
	float3 v1 = input[1].positionWS.xyz - v0;
	float3 v2 = input[2].positionWS.xyz - v0;
	float3 normal = cross(v1, v2);
	//normal = normalize(normal);
	
	#if DIRECTIONAL
		#ifndef DISABLE_DIRECTIONAL
		DirectionalShadows(input, normal, triStream);
		#endif
	#else
	PointShadows(input, normal, triStream);
	#endif
}

float4 frag(G2F IN) : SV_TARGET
{
	return float4(0,0,0,0);
}