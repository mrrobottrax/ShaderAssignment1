#pragma shader_feature_local _TRIPLANAR_ON
#pragma shader_feature_local _ROTATE_90_ON

int3 GetMask(float3 normal)
{
	normal = abs(normal);

	float maxF = max(normal.x, max(normal.y, normal.z));
	int3 mask = int3(
		normal.x == maxF ? 1 : 0,
		normal.y == maxF ? 1 : 0,
		normal.z == maxF ? 1 : 0
	);

	return mask;
}

float4 SampleTriplanar(sampler2D tex, float4 st, float4 texelSize, float3 position, int3 mask)
{
	float2 scale = texelSize.xy * 16;

	#if _ROTATE_90_ON
	float2 uvUp = position.xz * scale;
	float2 uvForward = position.xy * scale;
	float2 uvRight = position.zy * scale;
	#else
	float2 uvUp = position.zx * scale;
	float2 uvForward = position.yx * scale;
	float2 uvRight = position.yz * scale;
	#endif

	float2 uv = uvUp * mask.y + uvForward * mask.z + uvRight * mask.x;
	return tex2D(tex, uv * st.xy + st.zw);
}