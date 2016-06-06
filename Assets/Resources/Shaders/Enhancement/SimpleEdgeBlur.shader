Shader "Custom/SimpleEdgeBlur" 
{
    Properties 
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _CamRes_Width ("_CamResWidth", Float) = 640.0
        _CamRes_Width ("_CamResHeight", Float) = 480.0

        _MotionBlurVec ("MotionBlurVector", Vector) = (0, 0, 0, 0)
        _EnableMotionBlur ("Enable Motion Blur", Float) = 1.0
     	_EnableEdgeAntiAliasing ("Enable Edge Antialiasing", Float) = 1.0
        _AAWeight ("AntiAliasing Weight", Range(0, 0.5)) = 0.25
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

			#define MOTIONBLUR_SIZE 4

			#define AA _EnableEdgeAntiAliasing
			#define MB _EnableMotionBlur
			#define W _CamRes_Width
			#define H _CamRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform float2 _MotionBlurVec;
			uniform float _CamRes_Width;
			uniform float _CamRes_Height;
			uniform float _EnableMotionBlur;
			uniform float _EnableEdgeAntiAliasing;
			uniform float _AAWeight;

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
				// Sample for motion blur
				fixed4 blurcol = tex2D(_MainTex, i.uv - _MotionBlurVec);
				blurcol.a = 0.5 * (int)blurcol.a;

				// Render actual obj
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed x = (int)col.a;

				col = lerp(col, blurcol, blurcol.a * MB);
				col += blurcol * inv(x) * MB;

				// HORIZONTAL/VERTICAL EDGE BLUR (4 texture fetches)
				fixed4 temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, -1.0/H)); 
				fixed4 avgcol = temp * temp.a;

				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 0)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, 0)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, 1.0/H)); avgcol += temp * temp.a;

				// DIAGONAL SAMPLING (4 texture fetches)
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * temp.a;	
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, -1.0/H)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 1.0/H)); avgcol += temp * temp.a;

				fixed weight = avgcol.a;
				avgcol /= weight;
				fixed4 outcol = fixed4(col.rgb * x + avgcol.rgb * inv(x), x + (weight*_AAWeight) * inv(x) * AA);

				return outcol;
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}