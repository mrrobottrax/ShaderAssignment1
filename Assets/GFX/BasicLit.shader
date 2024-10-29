// This defines a simple unlit Shader object that is compatible with a custom Scriptable Render Pipeline.
// It applies a hardcoded color, and demonstrates the use of the LightMode Pass tag.
// It is not compatible with SRP Batcher.

Shader "BasicLit"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			// The value of the LightMode Pass tag must match the ShaderTagId in ScriptableRenderContext.DrawRenderers
			Tags { 
				"LightMode" = "Forward"
				"TerrainCompatible" = "True"
			}

			Cull Off
			ZTest Lequal
			Stencil
			{
				Ref 0
				Comp Equal
			}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 _AmbientColor;
			float4 _Color;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4x4 unity_MatrixVP;
			float4x4 unity_ObjectToWorld;
			float4 _WorldSpaceLightPos0;

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS		: NORMAL;
				float2 uvs			: TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS 	: SV_POSITION;
				float3 normalWS		: NORMAL;
				float2 uvs			: TEXCOORD0;
			};

			Varyings vert (Attributes IN)
			{
				Varyings OUT;
				float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);
				OUT.normalWS = mul(unity_ObjectToWorld, IN.normalOS);
				OUT.positionCS = mul(unity_MatrixVP, worldPos);
				OUT.uvs = IN.uvs;
				return OUT;
			}

			float4 frag (Varyings IN) : SV_TARGET
			{
				float4 color = _AmbientColor * _Color * tex2D(_MainTex, IN.uvs * _MainTex_ST.xy + _MainTex_ST.zw).rgba;

				if (color.a < 0.5) discard;

				return color;
			}
			ENDHLSL
		}

		Pass
		{
			// The value of the LightMode Pass tag must match the ShaderTagId in ScriptableRenderContext.DrawRenderers
			Tags { "LightMode" = "ShadowCaster"}

			Cull off
			ZTest Less
			ZWrite Off
			Blend Zero One

			Stencil
			{
				Comp Always

				ZFailBack IncrWrap
				ZFailFront DecrWrap
			}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			// Per frame
			float4x4 unity_MatrixVP;
			float4x4 unity_ObjectToWorld;
			float4 _WorldSpaceLightPos0;

			// Per mesh
			Buffer<float3> _VertexBuffer;
			Buffer<uint> _AdjacentBuffer;

			// Per submesh
			uint indexOffset;

			struct ATTRIB
			{
				float4 positionOS	: POSITION;
			};

			struct V2G
			{
				float4 positionWS	: POSITIONT;
			};

			struct G2F
			{
				float4 positionCS	: SV_POSITION;
				//float3 colour		: TEXCOORD0;
			};

			V2G vert(ATTRIB IN)
			{
				V2G OUT;
				float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);

				OUT.positionWS = worldPos;
				return OUT;
			}

			[maxvertexcount(30)]
			void geom(triangle V2G input[3], inout TriangleStream<G2F> triStream, uint id : SV_PrimitiveID)
			{
				G2F output;

				// Calculate the normal for the main triangle
				float3 v0 = input[0].positionWS.xyz;
				float3 v1 = input[1].positionWS.xyz - v0;
				float3 v2 = input[2].positionWS.xyz - v0;
				float3 normal = cross(v1, v2);
				normal = normalize(normal);

				// Check if the face is in shadow
				float d = dot(normal, _WorldSpaceLightPos0);
				if (d < 0)
				//if (1)
				{
					// Face in shadow

					int index = id * 3 + indexOffset;
					//float4 adj0 = mul(unity_ObjectToWorld, float4(vBuffer[adjacentBuffer[index + 0]], 1));
					//float4 adj1 = mul(unity_ObjectToWorld, float4(vBuffer[adjacentBuffer[index + 1]], 1));
					//float4 adj2 = mul(unity_ObjectToWorld, float4(vBuffer[adjacentBuffer[index + 2]], 1));

					//if (id != 0) return;

					float4 worldPos;

					// Reverse faces for front cap
					for (int i = 2; i >= 0; --i)
					{
						worldPos = input[i].positionWS;
						output.positionCS = mul(unity_MatrixVP, worldPos);
						// if (_AdjacentBuffer[index + i] == 4294967295)
						// 	output.colour = float3(1, 0, 0);
						// else
						// 	output.colour = 0;

						// output.colour += float3(0, 0, i / 3.0);

						//output.colour = float3(1,1,1);
						triStream.Append(output);
					}

					float4 offset = float4((-_WorldSpaceLightPos0 * 10000).xyz, 0);

					//triStream.RestartStrip();

					worldPos = input[0].positionWS + offset;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[2].positionWS;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[2].positionWS + offset;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[1].positionWS + offset;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[0].positionWS + offset;
					//worldPos = float4(0,10,0,1);
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[1].positionWS;
					//worldPos = float4(0,10,0,1);
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					triStream.RestartStrip();

					worldPos = input[1].positionWS;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[2].positionWS;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);

					worldPos = input[1].positionWS + offset;
					output.positionCS = mul(unity_MatrixVP, worldPos);
					//output.colour = 0;
					triStream.Append(output);
				}
				else
				{
					// Face out shadow, don't draw
				}
			}


			float4 frag(G2F IN) : SV_TARGET
			{
				return float4(0,0,0,0);
			}
			ENDHLSL
		}
	}
}