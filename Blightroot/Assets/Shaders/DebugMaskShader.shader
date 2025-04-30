Shader "Hidden/DebugMaskShader"
{
    Properties { _DebugMask("Mask", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct app { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _DebugMask;

            v2f vert(app v)
            {
                v2f o;
                o.pos = float4((v.vertex.xy*2-1),0,1);
                o.uv  = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_DebugMask, i.uv);
            }
            ENDCG
        }
    }
}
