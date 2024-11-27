cbuffer UnityPerFrame
{
	float4x4 unity_MatrixVP;
	float4 _AmbientColor;
};

cbuffer UnityPerDraw
{
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	float4 unity_WorldTransformParams;
};

float3 ObjToWorld(float3 obj)
{
	return mul((float3x4)unity_ObjectToWorld, float4(obj, 1));
}

float3 ObjToWorld_Direction(float3 obj)
{
	return normalize(mul((float3x3)unity_ObjectToWorld, obj));
}

float4 WorldToHClip(float3 world)
{
	return mul(unity_MatrixVP, float4(world, 1));
}

float4 ObjToHClip(float3 obj)
{
	return WorldToHClip(ObjToWorld(obj));
}

float3 UnpackNormal(float4 packedNormal)
{
	float3 normal;
	normal.xy = packedNormal.wy * 2 - 1;
	normal.z = sqrt(1 - normal.x * normal.x - normal.y * normal.y);
}