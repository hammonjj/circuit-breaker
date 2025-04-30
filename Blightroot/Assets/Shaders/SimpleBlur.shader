Shader "Custom/SimpleBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float    _BlurSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                // five‐tap blur
                fixed4 c = tex2D(_MainTex, uv) * 0.2;
                c += tex2D(_MainTex, uv + float2(_BlurSize, 0)) * 0.2;
                c += tex2D(_MainTex, uv + float2(-_BlurSize, 0)) * 0.2;
                c += tex2D(_MainTex, uv + float2(0, _BlurSize)) * 0.2;
                c += tex2D(_MainTex, uv + float2(0, -_BlurSize)) * 0.2;
                return c;
            }
            ENDCG
        }
    }
}
