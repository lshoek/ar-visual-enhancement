Shader "Custom/Glow"
{
    Properties 
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _GlowColor ("Glow Color", Color) = (1.0, 1.0, 1.0, 0.0)
        _GlowAmount ("Glow Amount", Range(0, 0.05)) = 0.005
        _Coeff ("_Coefficient", Range(0, 1.0)) = 0.35
    }
    
    Category 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque"}

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off
        Cull Off

        SubShader   
        {
            Pass 
            {
                CGPROGRAM
                #pragma vertex vert_img
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _Color;
                float4 _GlowColor;
                float _GlowAmount;
                float _Coeff;


                fixed4 frag (v2f_img i) : COLOR
                {
                    fixed4 texcol = tex2D(_MainTex, float2(i.uv.x, i.uv.y));
                    float coeff = _Coeff * (1.0 - texcol.a);

                    texcol += tex2D(_MainTex, float2(i.uv.x, i.uv.y - _GlowAmount)) * coeff;
                    texcol += tex2D(_MainTex, float2(i.uv.x - _GlowAmount, i.uv.y)) * coeff;
                    texcol += tex2D(_MainTex, float2(i.uv.x + _GlowAmount, i.uv.y)) * coeff;
                    texcol += tex2D(_MainTex, float2(i.uv.x, i.uv.y + _GlowAmount)) * coeff;

                    texcol.rgb += 1.0 * coeff;
                    return texcol;
                }
                ENDCG  
            }
        } 
    }
}