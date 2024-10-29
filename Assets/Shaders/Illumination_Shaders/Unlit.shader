Shader "Ethan/Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; // Object space position
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Homogeneous clip-space position
                float2 uv : TEXCOORD0;
            };

            // Declare the base texture
            sampler2D _MainTex;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Transform the object space position to homogeneous clip space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Pass the UV to the fragment shader
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the texture
                half4 texColor = tex2D(_MainTex, IN.uv);
                
                return texColor;
            }

            ENDHLSL
        }
    }
}