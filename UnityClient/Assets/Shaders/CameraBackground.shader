Shader "TransparentEarth/CameraBackground"
{
    Properties
    {
        _MainTex ("Camera", 2D) = "black" {}
        _QuarterTurns ("Clockwise quarter turns", Float) = 0
        _MirrorY ("Mirror vertically", Float) = 0
        _UvScale ("Cover crop", Vector) = (1, 1, 0, 0)
        _UvOffset ("Cover offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _QuarterTurns;
            float _MirrorY;
            float2 _UvScale;
            float2 _UvOffset;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float2 uv = input.uv * _UvScale + _UvOffset;
                if (_QuarterTurns < .5) output.uv = uv;
                else if (_QuarterTurns < 1.5) output.uv = float2(1.0 - uv.y, uv.x);
                else if (_QuarterTurns < 2.5) output.uv = 1.0 - uv;
                else output.uv = float2(uv.y, 1.0 - uv.x);
                if (_MirrorY > .5) output.uv.y = 1.0 - output.uv.y;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target { return tex2D(_MainTex, input.uv); }
            ENDCG
        }
    }
}
