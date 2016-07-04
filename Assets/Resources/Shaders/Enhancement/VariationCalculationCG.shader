Shader "Custom/VariationCalculationCG"
{
	Properties 
    {
		_Texture_Ref_Frame ("ReferenceFrame", 2D) = "white" {}
		_Texture_Avg_Frame ("AverageFrame", 2D) = "white" {}
    }
   
    SubShader 
    {   
        ZWrite On
        Cull Off

        Pass 
        {
			CGPROGRAM
            #pragma vertex VERT
            #pragma fragment FRAG
            #include "UnityCG.cginc"

			uniform sampler2D _Texture_Ref_Frame;
			uniform sampler2D _Texture_Avg_Frame;

            struct v2f
            {
                float4 pos : SV_POSITION;
                half2 uv0 : TEXCOORD0;
            };

            v2f VERT(appdata_img v)
            {
                v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
                o.uv0 = v.texcoord;
                return o;
            }
           
            fixed4 FRAG(v2f i) : COLOR
            {
            	fixed4 ref_col = tex2D (_Texture_Ref_Frame, i.uv0);
            	fixed4 avg_col = tex2D (_Texture_Avg_Frame, i.uv0);
                fixed4 col = fixed4(0.5, 0.5, 0.5, 0.5) + fixed4(ref_col - avg_col) * 2.0;
                
				return fixed4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}
