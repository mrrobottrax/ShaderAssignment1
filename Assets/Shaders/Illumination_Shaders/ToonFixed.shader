Shader "Ethan/ToonFixed"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1) // Base color of the object
        _RampTex ("Ramp Texture", 2D) = "white"
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Homogeneous clip-space position
                float3 normalWS : TEXCOORD0; // Interpolated world space normal (for lighting)
            };

            // Declare the base texture and sampler
            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; // Base color property
            CBUFFER_END

            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Transform object space position to homogeneous clip-space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Pass the world space normal (we'll recompute it in the fragment shader)
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));

                return OUT;
            }

            // Fragment Shader
            half4 frag(Varyings IN) : SV_Target
            {
                // Fetch main light direction and color
                Light mainLight = GetMainLight();
                half3 lightDirWS = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;

                // Calculate Lambertian diffuse lighting (NdotL)
                half NdotL = saturate(dot(IN.normalWS, lightDirWS));

                // Sample the ramp texture using NdotL to get the correct shade
                half rampValue = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NdotL, 0)).r;

                // Multiply the base color by the ramp value and light color
                half3 finalColor = _BaseColor.rgb * lightColor * rampValue;
                
                // Return the final color with alpha
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}

