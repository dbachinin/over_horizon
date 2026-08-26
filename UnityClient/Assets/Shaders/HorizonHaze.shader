Shader "TransparentEarth/HorizonHaze"
{
    Properties
    {
        _HazeColor ("Haze", Color) = (0.56, 1.0, 0.84, 0.46)
        _HazeAmount ("Amount", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _HazeColor;
            float _HazeAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float distanceFromCenter = abs(i.uv.y - .5) * 2.0;
                float softBand = pow(saturate(1.0 - distanceFromCenter), 1.45);
                float denseCore = 1.0 - smoothstep(.0, .23, distanceFromCenter);
                fixed4 color = _HazeColor;
                color.a *= (softBand * .72 + denseCore * .48) * _HazeAmount;
                return color;
            }
            ENDCG
        }
    }
}
