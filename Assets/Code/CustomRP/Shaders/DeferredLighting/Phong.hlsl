float GetHighlight(float3 lightDir, float3 viewDir, float3 normal, float exponent)
{
	// Reflection of light source
	float3 reflection = reflect(lightDir, normal);

	float d = saturate(dot(reflection, viewDir));

	return pow(d, exponent);
}