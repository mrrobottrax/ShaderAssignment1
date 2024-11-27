Shader "LUT"
{
    Properties
    {
		_Colours ("LUT Height", Integer) = 32
		_LutTex0 ("LUT0", 3D) = "white" {}
		_LutTex1 ("LUT1", 3D) = "white" {}
		_LutBlend ("Blend", Range(0, 1)) = 1
		_Contribution ("Contribution", Range(0, 1)) = 1
	}
    SubShader
    {
        // No culling or depth
        Cull Off
		ZWrite Off
		ZTest Always

        Pass
        {
			Name "LUT"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			sampler2D _Color;

			cbuffer UnityPerMaterial
			{
				Texture3D _LutTex0;
				float4 _LutTex0_TexelSize;
				SamplerState sampler_LutTex0;

				Texture3D _LutTex1;
				float4 _LutTex1_TexelSize;
				SamplerState sampler_LutTex1;

				int _Colours;
				float _Contribution;
				float _LutBlend;
			};

            struct Attributes
            {
                float3 vertex 	: POSITION;
				float2 uv 		: TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex 	: SV_POSITION;
				float2 uv 		: TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

				OUT.vertex = float4(IN.vertex.xy * 2 - 1, 0, 1);
				OUT.uv = float2(IN.uv.x, 1 - IN.uv.y);

				return OUT;
            }

			float4 LookupColour(float3 colour, Texture3D lut, SamplerState samplerState, float4 texelSize)
			{
				float3 col = saturate(colour);

				float offset = 0.5 * texelSize.x;
				float3 lutPos = float3(col.r + offset, 1 - offset - col.g, col.b + offset);

				return lut.Sample(samplerState, lutPos);
			}

            float3 frag(Varyings IN) : SV_TARGET
            {
				float3 colour = tex2D(_Color, IN.uv).rgb;

				float3 lutColor = lerp(
					LookupColour(colour, _LutTex0, sampler_LutTex0, _LutTex0_TexelSize),
					LookupColour(colour, _LutTex1, sampler_LutTex1, _LutTex1_TexelSize),
					_LutBlend
				).rgb;

                return lerp(colour, lutColor, _Contribution);
            }
            ENDHLSL
        }
    }
}
