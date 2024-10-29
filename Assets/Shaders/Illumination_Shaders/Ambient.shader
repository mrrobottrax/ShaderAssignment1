// Ambient shader is a simple diffuse with ambient shader


Shader "Ethan/Ambient"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1) // Base color of the object
        _MainTex ("Base Texture", 2D) = "white" {} // Texture map
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
                float3 normalOS : NORMAL; // Object space normal
                float2 uv : TEXCOORD0; // Texture UV
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Homogeneous clip-space position
                float3 normalWS : TEXCOORD1; // World space normal
                float2 uv : TEXCOORD0; // UV for texturing
            };

            // Declare the base texture and sampler
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; // Declare the base color (modifiable in inspector)
            CBUFFER_END

            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Transform the object space position to homogeneous clip space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Transform the object space normal to world space
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));

                // Pass the UV to the fragment shader
                OUT.uv = IN.uv;
                return OUT;
            }
            // Fragment Shader
            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the base texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Fetch the main light in URP
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);

                // Normalize the world space normal
                half3 normalWS = normalize(IN.normalWS);

                // Calculate Lambertian diffuse lighting (NdotL)
                half NdotL = saturate(dot(normalWS, lightDir));

                // Calculate ambient lighting using spherical harmonics (SH)
                half3 ambientSH = SampleSH(normalWS);

                // Combine the base color and texture with the diffuse light
                half3 diffuse = texColor.rgb * _BaseColor.rgb * NdotL;

                // Combine diffuse lighting with ambient spherical harmonics lighting
                half3 finalColor = diffuse + ambientSH * texColor.rgb * _BaseColor.rgb;

                // Return the final color
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}

