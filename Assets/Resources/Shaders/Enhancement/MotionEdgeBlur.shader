Shader "Custom/MotionEdgeBlur" 
{
    Properties 
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _CamRes_Width ("_CamResWidth", Float) = 640.0
        _CamRes_Width ("_CamResHeight", Float) = 480.0

        _MotionBlurVec ("MotionBlurVector", Vector) = (0, 0, 0, 0)
     	_EnableEdgeAntiAliasing ("Enable Edge Antialiasing", Float) = 1.0
        _AAWeight ("AntiAliasing Weight", Range(0, 0.5)) = 0.15
    }
    
    SubShader 
    {
        Pass 
        {  
            CGPROGRAM
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			#define BLUR_SIZE 12
			#define BLUR_RANGE 0.85
			#define BLUR_STRENGTH 0.65
			#define BLUR_OFFSET 0

			#define AA _EnableEdgeAntiAliasing
			#define W _CamRes_Width
			#define H _CamRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform float2 _MotionBlurVec;
			uniform float _CamRes_Width;
			uniform float _CamRes_Height;
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

			// WARNING! BRANCHING! GET RID OF THIS! // natural number
			fixed nat(float f) { return (f > 0 && f < 1.0) ? 0 : 1.0; }

			fixed4 FRAG (v2f i) : COLOR
			{
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
				col = fixed4(col.rgb * x + avgcol.rgb * inv(x), x + (weight * _AAWeight) * inv(x) * AA);
				fixed aa = nat(col.a);

				// Motion blur
				fixed count = 0;
				fixed4 addcol = col;
				_MotionBlurVec *= BLUR_RANGE;
				for (int j = 0; j < BLUR_SIZE; j++)
				{
					weight = BLUR_STRENGTH + j * (inv(BLUR_STRENGTH)/BLUR_SIZE);
					temp = tex2D(_MainTex, i.uv + _MotionBlurVec * (BLUR_OFFSET + j * (1.0/BLUR_SIZE)));
					count += (int)temp.a;

					addcol.rgb +=
						lerp(col.rgb * x + temp.rgb * inv(x), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					col.rgb = col.rgb * inv(nat(temp.a)) +
					 	lerp(col.rgb * x + temp.rgb * inv(x), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					
					col.a += (temp.a * inv(weight)) * inv(col.a) * nat(temp.a);
				}
				addcol.rgb /= clamp(count, 1.0, BLUR_SIZE);
				col.rgb = addcol.rgb * inv(x) + col.rgb * x;
				col.rgb /= 1.12 * inv(aa) + 1.0 * aa; // fix oversaturated outlines
				return col;
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}