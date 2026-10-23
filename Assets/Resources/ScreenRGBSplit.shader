Shader "Hidden/DoubleStick/ScreenRGBSplit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Amount;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 direction = i.uv - 0.5;
                float2 offset = direction * _Amount;
                fixed4 centerSample = tex2D(_MainTex, i.uv);

                fixed4 color;
                color.r = tex2D(_MainTex, i.uv + offset).r;
                color.g = centerSample.g;
                color.b = tex2D(_MainTex, i.uv - offset).b;
                color.a = centerSample.a;
                return color;
            }
            ENDCG
        }
    }
}
