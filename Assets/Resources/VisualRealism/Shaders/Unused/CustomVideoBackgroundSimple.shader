Shader "Custom/CustomVideoBackgroundSimple" 
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ObjectTex ("ObjectTexture", 2D) = "white" {}
    }
    
    SubShader 
    {
        Tags { "Queue" = "Overlay" }
        Pass 
        {  
        	ZTest Always
   			ZWrite Off
   			Cull Off
			Lighting Off

            CGPROGRAM
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			#define W _ScreenRes_Width
			#define H _ScreenRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform sampler2D _ObjectTex;

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv0 : TEXCOORD0;
				half2 uv1 : TEXCOORD1;
				half2 uv2 : TEXCOORD2;
			};

			v2f VERT(appdata_img v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv0 = v.texcoord;
				o.uv1.x = v.texcoord.x;
				o.uv1.y = inv(v.texcoord.y);
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				fixed4 vidcol = tex2D(_MainTex, i.uv0);
				fixed4 objcol = tex2D(_ObjectTex, i.uv1);

				float temp = objcol.a;
				objcol.a = round(objcol.a) * objcol.a;

				fixed3 rescol = fixed3(lerp(vidcol.rgb, objcol.rgb, temp));
				return fixed4(rescol.rgb, 1.0);
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}
