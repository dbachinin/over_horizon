Shader "TransparentEarth/AtmosphericGrid"
{
    Properties
    {
        _BaseColor ("Earth", Color) = (0.016, 0.044, 0.033, 1)
        _GridColor ("Grid", Color) = (0.62, 0.96, 0.82, 1)
        _GridOpacity ("Grid opacity", Range(0, 1)) = 1
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
            struct v2f { float4 vertex : SV_POSITION; float3 worldNormal : TEXCOORD0; float3 viewDir : TEXCOORD1; };
            fixed4 _BaseColor;
            fixed4 _GridColor;
            float _GridOpacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float longitude = atan2(n.z, n.x) / 6.2831853 + 0.5;
                float latitude = asin(n.y) / 3.1415926 + 0.5;
                float2 uv = float2(longitude, latitude);
                float2 coarseCell = abs(frac(uv * float2(18.0, 9.0)) - .5);
                float2 fineCell = abs(frac(uv * float2(72.0, 36.0)) - .5);
                float coarse = 1.0 - smoothstep(.006, .018, min(coarseCell.x, coarseCell.y));
                float fine = 1.0 - smoothstep(.004, .014, min(fineCell.x, fineCell.y));
                float rim = 1.0 - saturate(dot(n, normalize(i.viewDir)));
                float horizonDensity = smoothstep(.48, .92, rim);
                float grid = (coarse * .075 + fine * horizonDensity * .06) * _GridOpacity;
                float fresnel = pow(rim, 3.2);
                fixed4 color = lerp(_BaseColor, _GridColor, saturate(grid + fresnel * .06));
                color.a = saturate(_BaseColor.a + grid * .025 + fresnel * .04);
                return color;
            }
            ENDCG
        }
    }
}
