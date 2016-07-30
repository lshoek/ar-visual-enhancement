Shader "Custom/MotionBlur" 
{
    Properties 
    {
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _MotionBlurVec ("MotionBlurVector", Vector) = (0, 0, 0, 0)
        [HideInInspector] _MotionBlurVecLenght ("MotionBlurVectorLength", Float) = 0
        [HideInInspector] _CamRes_Width ("_CamResWidth", Float) = 640.0
        [HideInInspector] _CamRes_Width ("_CamResHeight", Float) = 480.0

        _BLUR_SAMPLES ("BLUR SAMPLES", Range(1.0, 25.0)) = 9.0
        _BLUR_RANGE ("BLUR RANGE", Range(0.25, 5.0)) = 0.75
        _BLUR_OFFSET ("BLUR OFFSET", Range(-10.0, 10.0)) = -0.5
        _BLUR_THRESHOLD ("BLUR THRESHOLD", Range(0, 5.0)) = 2.0
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
			uniform float2 _MotionBlurVec;

			uniform half _MotionBlurVecLength;
			uniform half _CamRes_Width;
			uniform half _CamRes_Height;

			uniform half _BLUR_SAMPLES;
			uniform half _BLUR_RANGE;
			uniform half _BLUR_OFFSET;
			uniform half _BLUR_THRESHOLD;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				half2 r00;
				half2 r01;
				half2 r02;
				half2 r10;
				half2 r12;
				half2 r20;
				half2 r21;
				half2 r22;
			};

			v2f VERT (appdata_base v)
			{
				v2f o;
				o.r00 = v.texcoord + half2(-1.0/W, -1.0/H);
				o.r01 = v.texcoord + half2(0, -1.0);
				o.r02 = v.texcoord + half2(1.0/W, -1.0/H);
				o.r10 = v.texcoord + half2(-1.0/W, 0);
				o.r12 = v.texcoord + half2(1.0/W, 0);
				o.r20 = v.texcoord + half2(-1.0/W, 1.0/H); 
				o.r21 = v.texcoord + half2(0, 1.0/H);
				o.r22 = v.texcoord + half2(1.0/W, 1.0/H);

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				/*****
				 **
				 **		MOTION BLUR
				 **		x texture samples
				 **
				 *****/

				/** Define col and blur range **/
				fixed4 col;
				_MotionBlurVec *= _BLUR_RANGE;

				/** Sample along blur vector **/
				for (int j = 0; j < _BLUR_SAMPLES; j++)
					col += tex2D(_MainTex, i.uv + _MotionBlurVec * (_BLUR_OFFSET + j * (1.0/_BLUR_SAMPLES)));

				/** Perform divisions to compute the display color **/
				col.rgb /= col.a;
				col.a /= _BLUR_SAMPLES;

				/*****
				 **
				 **		ANTI-ALIASING (Discrete Sampling)
				 **		3x3 kernel, 9 texture samples
				 **
				 *****/
				
				/** Blur threshold **/
				half thres = lerp(0, 1.0, clamp(_MotionBlurVecLength/_BLUR_THRESHOLD, 0, 1.0));

				/** Sampling **/
				fixed4 temp = tex2D(_MainTex, i.uv) * 4.0;
				temp += tex2D(_MainTex, i.r01) * 2.0;
				temp += tex2D(_MainTex, i.r10) * 2.0;
				temp += tex2D(_MainTex, i.r12) * 2.0;
				temp += tex2D(_MainTex, i.r21) * 2.0;
				temp += tex2D(_MainTex, i.r00);
				temp += tex2D(_MainTex, i.r02);
				temp += tex2D(_MainTex, i.r20);
				temp += tex2D(_MainTex, i.r22);

				/** Apply weight to incremental color **/
				temp.rgb /= temp.a;
				temp.a /= 16.0;
				
				/** Edge anti-aliasing **/
				col = col * thres + fixed4(col.rgb * col.a + temp.rgb * inv(col.a), temp.a) * inv(thres);

				/** Return result **/
				return col;
			}
			ENDCG
        }
    } 
}