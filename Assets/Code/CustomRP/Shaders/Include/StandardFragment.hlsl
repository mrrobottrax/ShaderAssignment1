#pragma fragment frag

float4 frag(Varyings IN) : SV_TARGET
{
	#if _TRIPLANAR_ON
	float4 color = SampleTriplanar(_MainTex, _MainTex_ST, _MainTex_TexelSize, IN.positionWS, IN.mask);
	#else
	float4 color = tex2D(_MainTex, IN.uv * _MainTex_ST.xy + _MainTex_ST.zw);
	#endif

	color *= _Color;
	
	return color;
}