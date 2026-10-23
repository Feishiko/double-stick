Shader "Hidden/DoubleStick/ScreenScanline"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.18
        _LineSpacing ("Line Spacing (px)", Float) = 3
        _LineThickness ("Line Thickness", Range(0.01, 1)) = 0.35
        _ScrollSpeed ("Scroll Speed (px/s)", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _Intensity;
            float _LineSpacing;
            float _LineThickness;
            float _ScrollSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float screenY = i.uv.y * _ScreenParams.y;
                float coord = frac((screenY + _Time.y * _ScrollSpeed) / max(_LineSpacing, 0.0001));
                float scanlineMask = 1.0 - step(_LineThickness, coord);
                float alpha = scanlineMask * _Intensity * _Color.a;
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
