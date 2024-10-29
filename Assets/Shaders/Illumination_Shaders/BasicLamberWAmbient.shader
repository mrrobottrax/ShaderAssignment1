Shader "Ethan/BasicLamberWAmbient"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _Shininess("Shininess", Range(1, 100)) = 30
    }

    SubShader
    {
        Tags{ "Queue" = "Geometry" }
        Pass 
        {

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float4 _SpecularColor;
            float _Shininess;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionOS.xyz));
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Fetch the main light in URP
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);

                // Normalize the world space normal
                half3 normalWS = normalize(IN.normalWS);

                // Lambertian diffuse lighting
                half NdotL = saturate(dot(normalWS, lightDir));
                half3 diffuse = _Color.rgb * NdotL * mainLight.color;

                // Specular highlights
                half3 viewDir = normalize(IN.viewDirWS);
                half3 halfDir = normalize(lightDir + viewDir);
                half specFactor = pow(saturate(dot(normalWS, halfDir)), _Shininess);
                half3 specular = _SpecularColor.rgb * specFactor * mainLight.color;

                // Ambient light using UNITY_LIGHTMODEL_AMBIENT for global illumination
                half3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;
                
                // Combine ambient, diffuse, and specular components
                half3 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                half3 finalColor = ambient * texColor + diffuse + specular;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}