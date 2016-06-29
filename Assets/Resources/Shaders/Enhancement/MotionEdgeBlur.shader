Shader "Custom/MotionEdgeBlur" 
{
    Properties 
    {
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _CamRes_Width ("_CamResWidth", Float) = 640.0
        [HideInInspector] _CamRes_Width ("_CamResHeight", Float) = 480.0
        [HideInInspector] _MotionBlurVec ("MotionBlurVector", Vector) = (0, 0, 0, 0)

     	[Toggle] _EnableEdgeAntiAliasing ("Enable Edge Antialiasing", Float) = 1.0
        _AA_WEIGHT ("AA WEIGHT", Range(0, 0.5)) = 0.125
        _BLUR_SIZE ("BLUR SIZE", Range(1.0, 16.0)) = 13.0
        _BLUR_RANGE ("BLUR RANGE", Range(0.25, 5.0)) = 1.25
        _BLUR_STRENGTH ("BLUR STRENGTH", Range(0, 10.0)) = 2.5
        _BLUR_OFFSET ("BLUR OFFSET", Range(-10.0, 10.0)) = -0.5
    }
    
    SubShader 
    {
        Pass 
        {  
            CGPROGRAM
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			#define AA _EnableEdgeAntiAliasing
			#define W _CamRes_Width
			#define H _CamRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform float2 _MotionBlurVec;
			uniform half _CamRes_Width;
			uniform half _CamRes_Height;
			uniform half _EnableEdgeAntiAliasing;
			uniform half _AA_WEIGHT;

			uniform half _BLUR_SIZE;
			uniform half _BLUR_RANGE;
			uniform half _BLUR_STRENGTH;
			uniform half _BLUR_OFFSET;

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
				o.r00 = v.texcoord + half2(0, -1.0/H);
				o.r01 = v.texcoord + half2(-1.0/W, 0);
				o.r02 = v.texcoord + half2(1.0/W, 0);
				o.r10 = v.texcoord + half2(0, 1.0/H);
				o.r12 = v.texcoord + half2(-1.0/W, -1.0/H);
				o.r20 = v.texcoord + half2(1.0/W, -1.0/H); 
				o.r21 = v.texcoord + half2(-1.0/W, -1.0/H);
				o.r22 = v.texcoord + half2(-1.0/W, 1.0/H); 
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			fixed nat(float f) { return floor(f) == f; }

			fixed4 FRAG (v2f i) : COLOR
			{
				/** Store obj color **/
				fixed4 col = tex2D(_MainTex, i.uv);

				/** Check if current pixel is translucent (0>x<1.0) **/
				fixed aa = nat(col.a);

				/*****
				 **
				 **		ANTI-ALIASING
				 **		3x3 kernel, 9 texture samples
				 **
				 *****/

				fixed4 avgcol = col * col.a * 4.0;
				fixed4 temp;
				temp = tex2D(_MainTex, i.r00); avgcol += temp * temp.a * 2.0;
				temp = tex2D(_MainTex, i.r01); avgcol += temp * temp.a * 2.0;
				temp = tex2D(_MainTex, i.r02); avgcol += temp * temp.a * 2.0;
				temp = tex2D(_MainTex, i.r10); avgcol += temp * temp.a * 2.0;
				temp = tex2D(_MainTex, i.r12); avgcol += temp * temp.a;	
				temp = tex2D(_MainTex, i.r20); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, i.r21); avgcol += temp * temp.a;
				temp = tex2D(_MainTex, i.r22); avgcol += temp * temp.a;

				/** Apply weight to incremental color **/
				fixed weight = avgcol.a;
				avgcol /= weight;
				
				/** SIMPLE BLUR (FULL ANTI-ALIASING) **/
				col = fixed4(avgcol.rgb, col.a + (weight * _AA_WEIGHT) * inv(col.a) * AA); //col = fixed4(col.rgb * x + avgcol.rgb * inv(x), x + (weight * _AA_WEIGHT) * inv(x) * AA);

				/*****
				 **
				 **		MOTION BLUR
				 **		12 texture samples
				 **
				 *****/

				/** Gauss kernel (sigma 3.0) **/
				static const float kernel[13] = 
				{ 
					0.018816,	0.034474,	0.056577,	
					0.083173,	0.109523,	0.129188,	
					0.136498,	0.129188,	0.109523,	
					0.083173,	0.056577,	0.034474,
					0.018816
				};

				/** Setup the variables **/
				fixed count = 1.0;
				fixed4 addcol = col;
				_MotionBlurVec *= _BLUR_RANGE;

				/** Mix 13 color samples along the blur vector **/
				for (int j = 0; j < _BLUR_SIZE; j++)
				{
					weight = inv(_BLUR_STRENGTH * kernel[j]);
					temp = tex2D(_MainTex, i.uv + _MotionBlurVec * (_BLUR_OFFSET + j * (1.0/_BLUR_SIZE)));
					count += (int)temp.a;

					addcol.rgb +=
						lerp(col.rgb * col.a + temp.rgb * inv(col.a), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					col.rgb = col.rgb * inv(nat(temp.a)) + 
					  	lerp(col.rgb * col.a + temp.rgb * inv(col.a), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					col.a += (temp.a * inv(weight)) * inv(col.a) * nat(temp.a);
				}

				//** Separate object pixels from camera pixels **/
				addcol.rgb /= max(1.0, count);
				col.rgb = addcol.rgb * inv(col.a) + col.rgb * col.a;

				return col;
			}
			ENDCG
        }
    } 
}