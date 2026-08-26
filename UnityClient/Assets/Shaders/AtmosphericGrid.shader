Shader "TransparentEarth/AtmosphericGrid"
{
    Properties
    {
        _LandMask ("Land mask", 2D) = "black" {}
        _LandColor ("Land", Color) = (0.006, 0.012, 0.011, 0.91)
        _OceanColor ("Ocean window", Color) = (0.008, 0.072, 0.064, 0.48)
        _DeepColor ("Underwater depth", Color) = (0.002, 0.022, 0.023, 0.68)
        _CausticColor ("Water caustics", Color) = (0.4, 1.0, 0.77, 1)
        _GridColor ("Grid", Color) = (0.62, 0.96, 0.82, 1)
        _PulseColor ("Blueprint pulse", Color) = (1.0, 0.76, 0.28, 1)
        _ReferenceColor ("Geodetic references", Color) = (1.0, 0.83, 0.42, 1)
        _HazeColor ("Surface haze", Color) = (0.4, 0.78, 0.66, 1)
        _GridOpacity ("Grid opacity", Range(0, 1)) = 1
        _HazeAmount ("Haze amount", Range(0, 1)) = 1
        _ReferenceOpacity ("Reference opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 objectNormal : TEXCOORD2;
            };
            sampler2D _LandMask;
            fixed4 _LandColor;
            fixed4 _OceanColor;
            fixed4 _DeepColor;
            fixed4 _CausticColor;
            fixed4 _GridColor;
            fixed4 _PulseColor;
            fixed4 _ReferenceColor;
            fixed4 _HazeColor;
            float _GridOpacity;
            float _HazeAmount;
            float _ReferenceOpacity;
            float4x4 _EnuToEcef;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.objectNormal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 localN = normalize(i.objectNormal);
                float3 globalN = normalize(mul((float3x3)_EnuToEcef, localN));
                float longitude = atan2(globalN.y, globalN.x) / 6.2831853 + 0.5;
                float latitude = asin(globalN.z) / 3.1415926 + 0.5;
                float2 uv = float2(longitude, latitude);

                float landSample = tex2D(_LandMask, uv).r;
                float land = smoothstep(.34, .66, landSample);
                float time = _Time.y;
                float waveA = sin(longitude * 118.0 + latitude * 43.0 + time * .72);
                float waveB = sin(longitude * 71.0 - latitude * 96.0 - time * .54);
                float waveC = sin((longitude + latitude) * 157.0 + time * .31);
                float waves = waveA * .46 + waveB * .34 + waveC * .20;
                float caustics = pow(saturate(.48 + waves * .42), 4.2);

                float2 texel = float2(.00195, .0039);
                float nearbyLand = 0.0;
                nearbyLand += tex2D(_LandMask, uv + float2(texel.x, 0)).r;
                nearbyLand += tex2D(_LandMask, uv - float2(texel.x, 0)).r;
                nearbyLand += tex2D(_LandMask, uv + float2(0, texel.y)).r;
                nearbyLand += tex2D(_LandMask, uv - float2(0, texel.y)).r;
                nearbyLand *= .25;
                float coast = saturate(abs(nearbyLand - landSample) * 3.2);

                fixed4 ocean = lerp(_DeepColor, _OceanColor, .52 + waves * .13);
                ocean.rgb += _CausticColor.rgb * caustics * (.055 + coast * .18);
                ocean.a += caustics * .025;
                fixed4 landColor = _LandColor;
                float waterShadow = coast * (.45 + .55 * sin(time * .8 + longitude * 93.0 + latitude * 77.0));
                landColor.rgb *= 1.0 - waterShadow * .30;
                landColor.rgb += _CausticColor.rgb * caustics * coast * .035;
                fixed4 color = lerp(ocean, landColor, land);

                float2 coarseCell = abs(frac(uv * float2(18.0, 9.0)) - .5);
                float2 fineCell = abs(frac(uv * float2(72.0, 36.0)) - .5);
                float coarse = 1.0 - smoothstep(.006, .018, min(coarseCell.x, coarseCell.y));
                float fine = 1.0 - smoothstep(.004, .014, min(fineCell.x, fineCell.y));
                float rim = 1.0 - saturate(dot(n, normalize(i.viewDir)));
                float horizonDensity = smoothstep(.48, .92, rim);
                float grid = (coarse * .075 + fine * horizonDensity * .06) * _GridOpacity;
                float fresnel = pow(rim, 3.2);
                color.rgb = lerp(color.rgb, _GridColor.rgb, saturate(grid + fresnel * .045));
                color.a = saturate(color.a + grid * .025 + fresnel * .035);

                float equator = 1.0 - smoothstep(.0004, .0012, abs(latitude - .5));
                float tropicOffset = 23.436 / 180.0;
                float tropics = max(1.0 - smoothstep(.0004, .0012, abs(latitude - (.5 + tropicOffset))),
                                    1.0 - smoothstep(.0004, .0012, abs(latitude - (.5 - tropicOffset))));
                float primeMeridian = 1.0 - smoothstep(.0004, .0012, abs(longitude - .5));
                float dateLine = 1.0 - smoothstep(.0004, .0012, min(longitude, 1.0 - longitude));
                float technicalDash = .58 + .42 * step(.35, frac((longitude + latitude) * 96.0));
                float references = saturate(max(max(equator, tropics), max(primeMeridian, dateLine))) *
                                   technicalDash * _ReferenceOpacity;
                color.rgb = lerp(color.rgb, _ReferenceColor.rgb, references * .82);
                color.a = saturate(color.a + references * .15);

                float scanDistance = acos(clamp(localN.y, -1.0, 1.0)) / 3.1415926;
                float scanPhase = frac(time * .25424) * 1.18;
                float scanWidth = lerp(.055, .009, saturate(scanDistance));
                float scanPulse = (1.0 - smoothstep(scanWidth * .16, scanWidth, abs(scanDistance - scanPhase))) *
                                  step(scanPhase, 1.0);
                float scanTrace = scanPulse * (.42 + grid * 2.8 + coast * .75);
                color.rgb = lerp(color.rgb, _PulseColor.rgb, saturate(scanTrace));
                color.a = saturate(color.a + scanPulse * .11);

                float surfaceHaze = smoothstep(.52, .96, rim) * _HazeAmount;
                float hazeBreakup = .92 + .08 * sin(longitude * 29.0 - latitude * 17.0 + time * .12);
                color.rgb = lerp(color.rgb, _HazeColor.rgb, surfaceHaze * hazeBreakup * .58);
                color.a = saturate(color.a + surfaceHaze * .28);
                return color;
            }
            ENDCG
        }
    }
}
