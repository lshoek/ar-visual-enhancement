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
        _BLUR_SIZE ("BLUR SIZE", Range(1.0, 16.0)) = 12.0
        _BLUR_RANGE ("BLUR RANGE", Range(0.25, 5.0)) = 2.0
        _BLUR_STRENGTH ("BLUR STRENGTH", Range(0, 10.0)) = 3.0
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
			uniform float _CamRes_Width;
			uniform float _CamRes_Height;
			uniform float _EnableEdgeAntiAliasing;
			uniform float _AA_WEIGHT;

			uniform float _BLUR_SIZE;
			uniform float _BLUR_RANGE;
			uniform float _BLUR_STRENGTH;
			uniform float _BLUR_OFFSET;

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

			// WARNING! BRANCHING! GET RID OF THIS! // natural number
			fixed nat(float f) { return (f > 0 && f < 1.0) ? 0 : 1.0; }

			fixed4 FRAG (v2f i) : COLOR
			{
				// gauss kernel
				float kernel[13] = 
				{ 
					0.018816,	0.034474,	0.056577,	
					0.083173,	0.109523,	0.129188,	
					0.136498,	0.129188,	0.109523,	
					0.083173,	0.056577,	0.034474,
					0.018816
				};

				// Store obj color
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed x = (int)col.a;

				// Edge AntiAliasing Horizontal/Vertical sampling (4 texture fetches
				fixed4 avgcol = col * x * 4.0;
				fixed4 temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, -1.0/H)); avgcol += temp * (int)temp.a * 2.0;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 0)); avgcol += temp * (int)temp.a * 2.0;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, 0)); avgcol += temp * (int)temp.a * 2.0;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, 1.0/H)); avgcol += temp * (int)temp.a * 2.0;

				// Diagonal sampling (4 texture fetches)
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * (int)temp.a;	
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, -1.0/H)); avgcol += temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 1.0/H)); avgcol += temp * (int)temp.a;

				fixed weight = (int)avgcol.a;
				avgcol /= weight;
				col = fixed4(col.rgb * x + avgcol.rgb * inv(x), x + (weight * _AA_WEIGHT) * inv(x) * AA);
				fixed aa = nat(col.a);

				// Motion blur
				fixed count = 0;
				fixed4 addcol = col;
				_MotionBlurVec *= _BLUR_RANGE;
				for (int j = 0; j < _BLUR_SIZE; j++)
				{
					weight = inv(_BLUR_STRENGTH * kernel[j]);
					temp = tex2D(_MainTex, i.uv + _MotionBlurVec * (_BLUR_OFFSET + j * (1.0/_BLUR_SIZE)));
					count += (int)temp.a;

					addcol.rgb +=
						lerp(col.rgb * x + temp.rgb * inv(x), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					col.rgb = col.rgb * inv(nat(temp.a)) + 
					  	lerp(col.rgb * x + temp.rgb * inv(x), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					col.a += (temp.a * inv(weight)) * inv(col.a) * nat(temp.a);
				}
				addcol.rgb /= clamp(count, 1.0, _BLUR_SIZE);
				col.rgb = addcol.rgb * inv(x) + col.rgb * x;
				col.rgb /= 1.25 * inv(aa) + aa; // fix oversaturated outlines in blur
				return col;
			}
			ENDCG
        }
    } 
}