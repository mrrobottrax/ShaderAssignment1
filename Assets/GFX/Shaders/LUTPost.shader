Shader "Custom/LUTPost"
{
	Properties
	{
		_LutTex ("LUT", 2D) = "white" {}
		_LutHeight ("LUT Height", Integer) = 32
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

			Texture2D _LutTex;
			SamplerState sampler_LutTex;

			int _LutHeight;

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

			float4 LookupColour(float3 colour)
			{
				int b = colour.b * _LutHeight;
				colour = saturate(colour);
				return _LutTex.Sample(sampler_LutTex, float2(((float)b + colour.r) / (float)_LutHeight, colour.g));
			}

			float4 frag(Varyings input) : SV_Target
			{
				float4 colour = tex2D(_BlitTexture, input.uv);

				return LookupColour(colour);
			}
			
			ENDHLSL
		}
	}
}
