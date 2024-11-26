Shader "Hidden/BG"
{
    Properties
    {}
    SubShader
    {
        // No culling or depth
        Cull Off
		ZWrite Off
		ZTest Equal

        Pass
        {
			Name "Background"

			Stencil
			{
				Ref 0
				Comp Equal
			}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 vertex 	: POSITION;
            };

            struct Varyings
            {
                float4 vertex 	: SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

				OUT.vertex = float4(IN.vertex.xy * 2 - 1, 0, 1);

				return OUT;
            }

            float3 frag(Varyings IN) : SV_TARGET
            {
                return float3(0.1, 0.1, 0.1);
            }
            ENDHLSL
        }
    }
}
