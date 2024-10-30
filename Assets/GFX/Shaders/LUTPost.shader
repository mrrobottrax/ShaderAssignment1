Shader "Custom/LUTPost"
{
	Properties
	{
		_Colours ("LUT Height", Integer) = 32
		_LutTex0 ("LUT0", 2D) = "white" {}
		_LutTex1 ("LUT1", 2D) = "white" {}
		_LutBlend ("Blend", Range(0, 1)) = 1
		_Contribution ("Contribution", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			Name "Blit"
			
			// Render State
			Cull Off
			Blend Off
			ZTest Off
			ZWrite Off
			
			HLSLPROGRAM
			
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			// Screen colour
			sampler2D _BlitTexture;

			Texture2D _LutTex0;
			float4 _LutTex0_TexelSize;
			SamplerState sampler_LutTex0;

			Texture2D _LutTex1;
			float4 _LutTex1_TexelSize;
			SamplerState sampler_LutTex1;

			int _Colours;
			float _Contribution;
			float _LutBlend;

			// Big tri that covers whole screen
			static float4 positions[] = {
				float4(-1, -1, 0, 1),
				float4(5, -1, 0, 1),
				float4(-1, 5, 0, 1),
			};

			static float2 uvs[] = {
				float2(0, 0),
				float2(3, 0),
				float2(0, -3),
			};

			struct Attributes
			{
				uint vertexID : SV_VERTEXID;
			};

			struct Varyings
			{
				float4 position : SV_POSITION;
				float2 uv		: TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				output.position = positions[input.vertexID];
				output.uv = uvs[input.vertexID];
				return output;
			}

			float4 LookupColour(float3 colour, Texture2D lut, SamplerState samplerState, float4 texelSize)
			{
				float maxColour = _Colours - 1.0;
				float4 col = float4(saturate(colour), 1);

				float halfColX = 0.5 / texelSize.z;
				float halfColY = 0.5 / texelSize.w;
				float threshold = maxColour / _Colours;

				float xOffset = halfColX + col.r * threshold / _Colours;
				float yOffset = halfColY + col.g * threshold;
				float cell = floor(col.b * maxColour);

				float2 lutPos = float2(cell / _Colours + xOffset, yOffset);

				return lut.Sample(samplerState, lutPos);
			}

			float4 frag(Varyings input) : SV_Target
			{
				float4 colour = tex2D(_BlitTexture, input.uv);

				float4 lutColor = lerp(
					LookupColour(colour, _LutTex0, sampler_LutTex0, _LutTex0_TexelSize),
					LookupColour(colour, _LutTex1, sampler_LutTex1, _LutTex1_TexelSize),
					_LutBlend
				);

				return lerp(colour, lutColor, _Contribution);
			}
			
			ENDHLSL
		}
	}
}
