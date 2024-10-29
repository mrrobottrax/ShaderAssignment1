Shader "Ethan/SimpleTint"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _TintColour ("Tint Color", Color) = (1, 1, 1, 1) // Tint colour applied to  the object
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; // Object space position
                float2 uv : TEXCOORD0; // Object space normal
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Homogeneous clip-space position
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _TintColour;


            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Transform object space position to homogeneous clip-space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Pass the uv
                OUT.uv = IN.uv;

                return OUT;
            }

            // Fragment Shader
            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                return col * _TintColour;

            }
            ENDHLSL
        }
    }
}
