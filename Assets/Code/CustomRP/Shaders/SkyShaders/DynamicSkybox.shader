Shader "CustomRP/DynamicSkybox"
{
    Properties
    {
        [Header(Sun Properties)]
        _SunColor("Sun Color", Color) = (1,1,1,1)
        _SunSize("Sun Size",  Range(0, 2)) = 0.1
        _SunTex("Sun Texture", 2D) = "black" {}

        [Header(Moon Properties)]
        _MoonColor("Moon Color", Color) = (1,1,1,1)
        _MoonSize("Moon Size",  Range(0, 2)) = 0.15
        _MoonTex("Sun Texture", 2D) = "black" {}

        [Header(Day Sky Settings)]
        _DayTopColor("Day Sky Color Top", Color) = (0.4,1,1,1)
        _DayBottomColor("Day Sky Color Bottom", Color) = (0,0.8,1,1)

        [Header(Night Sky Settings)]
        _NightTopColor("Night Sky Color Top", Color) = (0,0,0,1)
        _NightBottomColor("Night Sky Color Bottom", Color) = (0,0,0.2,1)

        [Header(Stars Settings)]
        _Stars("Stars Texture", 2D) = "black" {}
        _StarsCutoff("Stars Cutoff",  Range(0, 1)) = 0.08
        _StarsSpeed("Stars Move Speed",  Range(0, 1)) = 0.3 

        [Header(Horizon Settings)]
        _OffsetHorizon("Horizon Offset",  Range(-1, 1)) = 0
        _HorizonIntensity("Horizon Intensity",  Range(0, 10)) = 3.3
        _SunSet("Sunset/Rise Color", Color) = (1,0.8,1,1)
        _HorizonColorDay("Day Horizon Color", Color) = (0,0.8,1,1)
        _HorizonColorNight("Night Horizon Color", Color) = (0,0.8,1,1)

        [Header(Time Of Day)]
        _IsDay("Is Day (1: Day, 0: Night)", Int) = 1
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Code/CustomRP/Shaders/Include/Core.hlsl"

            struct Appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            // Textures
            sampler2D _Stars, _SunTex, _MoonTex;

            // Sizes and Positions
            float _SunSize;
            float _MoonSize;
            float _OffsetHorizon;

            // Colors
            float4 _SunColor;
            float4 _MoonColor;
            float4 _DayTopColor;
            float4 _DayBottomColor;
            float4 _NightTopColor;
            float4 _NightBottomColor;
            float4 _HorizonColorDay;
            float4 _HorizonColorNight;
            float4 _SunSet;

            // Stars
            float _StarsCutoff;
            float _StarsSpeed;

            // Horizon
            float _HorizonIntensity;

            // Time
            float _Time;
            int _IsDay; // Use int for performance

            // Lighting and Matrices
            float4 _WorldSpaceLightPos0;

            Varyings vert(Appdata IN)
            {
                Varyings OUT;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;
                OUT.vertex = mul(unity_MatrixVP, float4(OUT.worldPos, 1));
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float horizon = abs((IN.uv.y * _HorizonIntensity) - _OffsetHorizon);

                // UV for the sky
                float2 skyUV = IN.worldPos.xz / IN.worldPos.y;
                float3 normalizedWorldPos = normalize(IN.worldPos);

                float belowHorizonFactor = saturate((_OffsetHorizon - normalizedWorldPos.y) * 20); // Fade factor below the horizon

                float3 sunDirection = (_IsDay == 1) ? _WorldSpaceLightPos0.xyz : -_WorldSpaceLightPos0.xyz;
                float3 moonDirection = -sunDirection;

                // Sun
                float sun = distance(IN.uv.xyz, sunDirection);
                float sunDisc = 1 - (sun / _SunSize);
                sunDisc = saturate(sunDisc * 50);
                sunDisc *= 1 - belowHorizonFactor;
                float3 sunFinal = sunDisc * _SunColor.rgb;

                // Moon
                float moon = distance(IN.uv.xyz, moonDirection);
                float moonDisc = 1 - (moon / _MoonSize);
                moonDisc = saturate(moonDisc * 50);
                moonDisc *= 1 - belowHorizonFactor;
                float3 moonFinal = moonDisc * _MoonColor.rgb;
                // Combine sun and moon
                float3 sunMoonFinal = sunFinal + moonFinal;

                // Stars
                float3 stars = tex2D(_Stars, skyUV + (_StarsSpeed * _Time.x)).rgb;
                stars *= 1 - saturate(sunDirection.y * 3);
                stars = step(_StarsCutoff, stars);
                stars *= 1 - belowHorizonFactor;
                float3 starsFinal = stars * (1 - sunDisc) * (1 - moonDisc);// Stop stars from affecting sun and moon

                // Gradient Skies
                float3 gradientDay = lerp(_DayBottomColor.rgb, _DayTopColor.rgb, saturate(horizon));
                float3 gradientNight = lerp(_NightBottomColor.rgb, _NightTopColor.rgb, saturate(horizon));

                float3 skyGradientsFinal = lerp(gradientNight, gradientDay, saturate(sunDirection.y));

                // Below horizon handling
                float3 belowHorizonColor = lerp(_NightBottomColor.rgb, _DayBottomColor.rgb, saturate(sunDirection.y));
                float3 finalSkyColor = lerp(skyGradientsFinal, belowHorizonColor, belowHorizonFactor);

                // Horizon Glow

                // Sunset glow
                float sunset = saturate((1 - horizon) * saturate(sunDirection.y * 5));
                float3 sunsetColoured = sunset * _SunSet.rgb;

                // Daytime horizon glow
                float3 horizonGlow = saturate((1 - horizon * 0.5) * saturate(sunDirection.y * 10)) * _HorizonColorDay.rgb;

                // Nighttime horizon glow
                float3 horizonGlowNight = saturate((1 - horizon * 0.5) * saturate(-sunDirection.y * 10)) * _HorizonColorNight.rgb;

                // Combine daytime and nighttime glows
                horizonGlow += horizonGlowNight;

                float3 finalOutput = finalSkyColor + sunsetColoured + starsFinal + horizonGlow + sunMoonFinal;

                return float4(finalOutput, 1);
            }

            ENDHLSL
        }
    }
}