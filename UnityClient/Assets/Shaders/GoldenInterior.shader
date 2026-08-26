Shader "TransparentEarth/GoldenInterior"
{
    Properties
    {
        _InteriorColor ("Golden interior", Color) = (0.92, 0.65, 0.20, 0.38)
        _PulseColor ("Scan pulse", Color) = (1.0, 0.86, 0.48, 1)
        _HazeAmount ("Haze amount", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-40" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

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
                float3 localNormal : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };
            fixed4 _InteriorColor;
            fixed4 _PulseColor;
            float _HazeAmount;
            float4x4 _EnuToEcef;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.localNormal = input.normal;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.viewDir = WorldSpaceViewDir(input.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 localN = normalize(input.localNormal);
                float3 worldN = normalize(input.worldNormal);
                float3 globalN = normalize(mul((float3x3)_EnuToEcef, localN));
                float longitude = atan2(globalN.y, globalN.x) / 6.2831853 + .5;
                float latitude = asin(globalN.z) / 3.1415926 + .5;
                float2 cell = abs(frac(float2(longitude, latitude) * float2(36.0, 18.0)) - .5);
                float blueprint = 1.0 - smoothstep(.008, .025, min(cell.x, cell.y));
                float depth = saturate(abs(dot(worldN, normalize(input.viewDir))));
                float scanDistance = acos(clamp(localN.y, -1.0, 1.0)) / 3.1415926;
                float scanPhase = frac(_Time.y * .25424) * 1.18;
                float scanWidth = lerp(.065, .012, saturate(scanDistance));
                float pulse = (1.0 - smoothstep(scanWidth * .16, scanWidth, abs(scanDistance - scanPhase))) *
                              step(scanPhase, 1.0);
                float strata = .5 + .5 * sin(latitude * 184.0 + longitude * 37.0 + _Time.y * .08);
                fixed4 color = _InteriorColor;
                color.rgb *= .56 + strata * .16 + blueprint * .34;
                color.rgb = lerp(color.rgb, _PulseColor.rgb, pulse * (.66 + blueprint * .34));
                color.a *= (.42 + depth * .30 + blueprint * .32 + pulse * .44) * (1.0 + _HazeAmount * .08);
                return color;
            }
            ENDCG
        }
    }
}
