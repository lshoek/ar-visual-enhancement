Shader "Custom/GaussianBlur3x3" 
{
    Properties 
    {
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _CamRes_Width ("_CamResWidth", Float) = 640.0
        [HideInInspector] _CamRes_Height ("_CamResHeight", Float) = 480.0
    }
    
    SubShader 
    {
        Pass 
        {  
            CGPROGRAM
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			#define W _CamRes_Width
			#define H _CamRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform half _CamRes_Width;
			uniform half _CamRes_Height;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half2 uv : TEXCOORD0;
				half2 r00 : TEXCOORD1;
				half2 r01 : TEXCOORD2;
				half2 r02 : TEXCOORD3;
				half2 r03 : TEXCOORD4;
			};

			v2f VERT (appdata_base v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.r00 = v.texcoord + half2(-0.33/W, -1.0/H);
				o.r01 = v.texcoord + half2(1.0/W, -0.33/H);
				o.r02 = v.texcoord + half2(0.33/W, 1.0/H);
				o.r03 = v.texcoord + half2(-1.0/W, 0.33/H);
				o.uv = v.texcoord;
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				/*****
				 **
				 **		ANTI-ALIASING (Linear Sampling)
				 **		3x3 kernel, 5 texture samples
				 **
				 *****/

			 	/** Sampling **/
				fixed4 temp = tex2D(_MainTex, i.uv) * 4.0;
				temp += tex2D(_MainTex, i.r00) * 3.0;
				temp += tex2D(_MainTex, i.r01) * 3.0;
				temp += tex2D(_MainTex, i.r02) * 3.0;
				temp += tex2D(_MainTex, i.r03) * 3.0;
				temp /= 16.0;

				/** Return result **/
				return temp;
			}
			ENDCG
        }
    } 
}