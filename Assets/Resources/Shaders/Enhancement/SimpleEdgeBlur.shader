Shader "Custom/SimpleEdgeBlur" 
{
    Properties 
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _CamRes_Width("_CamResWidth", Float) = 640.0
        _CamRes_Width("_CamResHeight", Float) = 480.0

     	_EnableEdgeAntiAliasing ("Enable Edge Antialiasing", Float) = 1.0
        _AA_Weight ("AntiAliasing Weight", Range(0, 0.5)) = 0.25
    }
    
    SubShader 
    {
        Pass 
        {  
            CGPROGRAM
			#pragma vertex VERT
			#pragma fragment FRAG
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

			#define E _EnableEdgeAntiAliasing
			#define W _CamRes_Width
			#define H _CamRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform float _CamRes_Width;
			uniform float _CamRes_Height;
			uniform float _EnableEdgeAntiAliasing;
			uniform float _AA_Weight;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f VERT (appdata_base v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed x = (int)col.a;

				// HORIZONTAL/VERTICAL EDGE BLUR (4 texture hor/ver fetches)
				fixed4 temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, -1.0/H)); 
				fixed4 avgcol = temp * temp.a;

				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 0)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, 0)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, 1.0/H)); avgcol += temp * temp.a;

				// DIAGONAL SAMPLING
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * temp.a;	
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, -1.0/H)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 1.0/H)); avgcol += temp * temp.a;

				fixed weight = avgcol.a;
				avgcol /= weight;

				// HORIZONTAL/VERTICAL EDGE BLUR (4 texture hor/ver fetches)
				// fixed4 p12 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, -1.0/H));		
				// fixed4 p21 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 0));
				// fixed4 p23 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, 0));		
				// fixed4 p32 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, 1.0/H));

				// // DIAGONAL SAMPLING
				// fixed4 p11 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H));		
				// fixed4 p13 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, -1.0/H));
				// fixed4 p31 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H));		
				// fixed4 p33 = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 1.0/H));

				// fixed weight = p12.a + p21.a + p23.a + p32.a + p11.a + p13.a + p31.a + p33.a;

				// fixed4 avgcol = (p12 * p12.a + p21 * p21.a + p23 * p23.a + p32 * p32.a + 
				// 	+ p11 * p11.a + p13 * p13.a + p31 * p31.a + p33 * p33.a) / weight;

				return fixed4(col.rgb * x + avgcol.rgb * inv(x), x + (weight*_AA_Weight) * inv(x) * E);
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}