Shader "Custom/MotionBlur" 
{
    Properties 
    {
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _MotionBlurVec ("MotionBlurVector", Vector) = (0, 0, 0, 0)
        [HideInInspector] _MotionBlurVecLenght ("MotionBlurVectorLength", Float) = 0
        [HideInInspector] _BLUR_SAMPLES ("BLUR SAMPLES", Range(1.0, 25.0)) = 11.0
        [HideInInspector] _BLUR_RANGE ("BLUR RANGE", Range(0.25, 5.0)) = 1.0
        [HideInInspector] _BLUR_OFFSET ("BLUR OFFSET", Range(-10.0, 10.0)) = -0.5
    }
    
    SubShader 
    {
        Pass 
        {  
            CGPROGRAM
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform float2 _MotionBlurVec;
			uniform half _MotionBlurVecLength;
			
			uniform half _BLUR_SAMPLES;
			uniform half _BLUR_RANGE;
			uniform half _BLUR_OFFSET;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half2 uv : TEXCOORD0;
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

				return col;
			}
			ENDCG
        }
    } 
}