Shader "Hidden/DeferredLighting"
{
    Properties
    {}
    SubShader
    {
        // No culling or depth
        Cull Off
		ZWrite Off
		ZTest Always

		Blend One One

        Pass
        {
			// Sun lighting

			Name "Directional Lit"

			Stencil
			{
				Ref 0
				Comp Equal
			}

            HLSLPROGRAM

			#include_with_pragmas "CGClass.hlsl"

			#ifdef SPECULAR
			float3 _CameraPos;
			sampler2D _Position;
			#endif

            #pragma vertex vert
            #pragma fragment frag

			sampler2D _Albedo;
			sampler2D _Normal;

			float4 _WorldSpaceLightPos0;
			float4 _LightColor;
			float4 _AmbientColor;

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

            float3 frag(Varyings IN) : SV_TARGET
            {
				float4 albedo = tex2D(_Albedo, IN.uv);
				float3 normal = tex2D(_Normal, IN.uv).rgb;

				float lambert = saturate(dot(_WorldSpaceLightPos0.xyz, normal));

				float3 lightColor = lerp(_AmbientColor.rgb, _LightColor.rgb, lambert);

				#if NO_LIGHTING
                return albedo.rgb;
				#endif

				#ifdef DIFFUSE
				float3 diffuse = lightColor * albedo.rgb * albedo.a;
				#endif
				
				#ifdef SPECULAR
				float3 position = tex2D(_Position, IN.uv).rgb;
				float3 viewDir = normalize(position - _CameraPos);
				float3 specular = GetHighlight(_WorldSpaceLightPos0.xyz, viewDir, normal, 16) * 0.5f;
				#endif

				#if DIFFUSE_SPECULAR
				return diffuse + specular;
				#endif

				#if DIFFUSE_ONLY
				return diffuse;
				#endif

				#if SPECULAR_ONLY
				return specular;
				#endif
            }
            ENDHLSL
        }

		Pass
        {
			// Shadows

			Name "Directional Ambient"

			Stencil
			{
				Ref 0
				Comp NotEqual
			}

            HLSLPROGRAM

			#include_with_pragmas "CGClass.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _Albedo;

			float4 _AmbientColor;

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

            float3 frag(Varyings IN) : SV_TARGET
            {
				float4 albedo = tex2D(_Albedo, IN.uv);

				#if NO_LIGHTING
                return albedo.rgb;
				#endif

				#ifdef DIFFUSE
				float3 diffuse = _AmbientColor.rgb * albedo.rgb * albedo.a;
				return diffuse;
				#endif

				#if SPECULAR_ONLY
				return 0;
				#endif
            }
            ENDHLSL
        }

		Pass
        {
			// Point light

			Name "Point"

			Stencil
			{
				Ref 0
				Comp Equal
			}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			sampler2D _Albedo;
			sampler2D _Position;
			sampler2D _Normal;

			float4 _WorldSpaceLightPos0;
			float4 _LightColor;
			float4 _LightDir;

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

			float3 GetPointLightColor(float3 positionWS, float3 normalWS)
			{
				// Dist to light
				float3 delta = positionWS - _WorldSpaceLightPos0.xyz;
				float dist = length(delta);
				float3 dir = delta / dist;

				// Falloff
				float brightness = 1 - dist / _LightDir.w;

				// Lambert
				float lambert = saturate(-dot(dir, normalWS));

				// Final colour
				float light = saturate(brightness * lambert);

				return light * _LightColor.rgb;
			}

            float3 frag(Varyings IN) : SV_TARGET
            {
				float3 albedo = tex2D(_Albedo, IN.uv).rgb;
				float3 position = tex2D(_Position, IN.uv).rgb;
				float3 normal = tex2D(_Normal, IN.uv).rgb;

				float3 lightColor = GetPointLightColor(position, normal);

                return lightColor * albedo;
            }
            ENDHLSL
        }

		Pass
        {
			// Spot light

			Name "Spot"

			Stencil
			{
				Ref 0
				Comp Equal
			}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			sampler2D _Albedo;
			sampler2D _Position;
			sampler2D _Normal;

			float4 _WorldSpaceLightPos0;
			float4 _LightColor;
			float4 _LightDir;
			float _TanTheta;

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

			float3 GetSpotLightColor(float3 positionWS, float3 normalWS)
			{
				// Dist to light
				float3 delta = positionWS - _WorldSpaceLightPos0.xyz;
				float dist = length(delta);
				float3 dir = delta / dist;

				// Forward falloff
				float forward = dot(delta, _LightDir.xyz);
				float forwardClip = saturate(dot(dir * 256, _LightDir.xyz));
				float brightness = saturate(1 - dist / _LightDir.w);
				brightness *= forwardClip;

				// Lambert
				float lambert = saturate(-dot(dir, normalWS));

				// Distance to centre ray
				float3 closestPoint = forward * _LightDir.xyz;
				float distPerp = distance(closestPoint, delta);

				// Angle spread
				float spread = _TanTheta * forward;
				float atten = saturate(1 - distPerp / spread);

				// Final colour
				float light = saturate(brightness * lambert * atten);

				return light * _LightColor.rgb;
			}

            float3 frag(Varyings IN) : SV_TARGET
            {
				float3 albedo = tex2D(_Albedo, IN.uv).rgb;
				float3 position = tex2D(_Position, IN.uv).rgb;
				float3 normal = tex2D(_Normal, IN.uv).rgb;

				float3 lightColor = GetSpotLightColor(position, normal);

                return lightColor * albedo;
            }
            ENDHLSL
        }
    }
}
