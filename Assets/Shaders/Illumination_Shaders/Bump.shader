Shader "Ethan/Bump"
{
    Properties
    {
        _myDiffuse ("Diffuse Texture", 2D) = "white" {} // Diffuse texture
        _myBump ("Bump Texture", 2D) = "bump" {} // Bump (normal) texture
        _mySlider ("Bump Amount", Range(0,10)) = 1 // Bump intensity slider
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

                float2 uv : TEXCOORD0; // UV coordinates for texturing
                
                float4 tangentOS : TANGENT; // Tangent for normal mapping
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Homogeneous clip-space position

                float3 normalWS : TEXCOORD1; // World space normal

                float3 tangentWS : TEXCOORD2; // World space tangent

                float2 uv : TEXCOORD0; // UV coordinates

                float3 bitangentWS : TEXCOORD3; // World space bitangent

                float3 viewDirWS : TEXCOORD4; // World space view direction

            };

            TEXTURE2D(_myDiffuse); // Declare the diffuse texture

            SAMPLER(sampler_myDiffuse); // Sampler for diffuse texture

            TEXTURE2D(_myBump); // Declare the bump texture

            SAMPLER(sampler_myBump); // Sampler for bump texture

            CBUFFER_START(UnityPerMaterial)
                float _mySlider; // Bump intensity slider
            CBUFFER_END

            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Transform object space position to homogeneous clip-space position
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Transform object space normal and tangent to world space
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.tangentWS = normalize(TransformObjectToWorldNormal(IN.tangentOS.xyz));
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;

                // Calculate view direction in world space
                float3 worldPosWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = normalize(GetCameraPositionWS() - worldPosWS);

                // Pass UV coordinates to the fragment shader
                OUT.uv = IN.uv;
                return OUT;
            }
            
            // Fragment Shader
            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the diffuse texture
                half4 albedo = SAMPLE_TEXTURE2D(_myDiffuse, sampler_myDiffuse, IN.uv);

                // Sample and unpack the normal map
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_myBump, sampler_myBump, IN.uv));

                // Apply the bump intensity slider to the normal map
                normalTS.xy *= _mySlider;

                // Transform tangent space normal to world space
                half3x3 TBN = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                half3 normalWS = normalize(mul(normalTS, TBN));

                // Fetch the main light and calculate its direction
                Light mainLight = GetMainLight();
                half3 lightDirWS = normalize(mainLight.direction);

                // Calculate the Lambertian (diffuse) component
                half NdotL = saturate(dot(normalWS, lightDirWS));
                half3 diffuse = albedo.rgb * NdotL;

                // Return the final color with diffuse shading
                return half4(diffuse, albedo.a);
            }
            ENDHLSL
        }
    }
}