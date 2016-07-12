Shader "Custom/MotionEdgeBlurClassic" 
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

			#define MOTIONBLUR_SIZE 4
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

				fixed4 temp;
				fixed weight;

				// Edge AntiAliasing Horizontal/Vertical sampling (4 texture fetches
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, -1.0/H)); fixed4 avgcol = temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 0)); avgcol += temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, 0)); avgcol += temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(0, 1.0/H)); avgcol += temp * (int)temp.a;

				// Diagonal sampling (4 texture fetches)
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * (int)temp.a;	
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(1.0/W, -1.0/H)); avgcol += temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, -1.0/H)); avgcol += temp * (int)temp.a;
				temp = tex2D(_MainTex, half2(i.uv.x, i.uv.y) + half2(-1.0/W, 1.0/H)); avgcol += temp * (int)temp.a;

				weight = avgcol.a;
				avgcol /= weight;
				col = fixed4(col.rgb * x + avgcol.rgb * inv(x), x + (weight * _AAWeight) * inv(x) * AA);

				// Motion blur
				_MotionBlurVec *= 1.25;
				for (int j = 0; j < MOTIONBLUR_SIZE; j++)
				{
					weight = 0.2 + j * 0.2;
					temp = tex2D(_MainTex, i.uv + _MotionBlurVec * (-0.15 + j * 0.25));
					col.rgb = col.rgb * inv(nat(temp.a)) + 
						lerp(col.rgb * x + temp.rgb * inv(x), temp.rgb, inv(weight) * temp.a) * nat(temp.a);
					col.a += (temp.a * inv(weight)) * inv(col.a) * nat(temp.a);
				}
				return col;
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}