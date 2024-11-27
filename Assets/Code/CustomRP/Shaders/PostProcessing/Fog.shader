Shader "Hidden/Fog"
{
    Properties
    {}
    SubShader
    {
        // No culling or depth
        Cull Off
		ZWrite Off
		ZTest NotEqual

        Pass
        {
			Name "Fog"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			float _FogStart;
			float _FogEnd;
			float4 _FogColor;

			float3 _CameraPos;

			sampler2D _Color;
			sampler2D _Position;

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
				float3 color = tex2D(_Color, IN.uv).rgb;
				float3 position = tex2D(_Position, IN.uv).rgb;

				float dist = distance(position, _CameraPos);

				// Scale intensity based on distance
				float fract = saturate((dist - _FogStart) / _FogEnd) * _FogColor.a;

				float3 finalColor = lerp(color, _FogColor.rgb, fract);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
