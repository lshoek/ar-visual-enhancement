Shader "Custom/AvgImageCalculationCG" 
{
    Properties 
    {
		_Texture_Frame_0 ("TextureFrame0", 2D) = "white" {}
		_Texture_Frame_1 ("TextureFrame1", 2D) = "white" {}
		_Texture_Frame_2 ("TextureFrame2", 2D) = "white" {}
        _Texture_Frame_3 ("TextureFrame3", 2D) = "white" {}
        _Texture_Frame_4 ("TextureFrame4", 2D) = "white" {}
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

			uniform sampler2D _Texture_Frame_0;
			uniform sampler2D _Texture_Frame_1;
			uniform sampler2D _Texture_Frame_2;
            uniform sampler2D _Texture_Frame_3;
            uniform sampler2D _Texture_Frame_4;

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
            	fixed4 col = tex2D (_Texture_Frame_0, i.uv0);
                col += tex2D (_Texture_Frame_1, i.uv0);
				col += tex2D (_Texture_Frame_2, i.uv0);
                col += tex2D (_Texture_Frame_3, i.uv0);
                col += tex2D (_Texture_Frame_4, i.uv0);
				col /= col.a;
				return fixed4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}
