﻿Shader "Custom/CustomVideoBackground" 
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ObjectTex ("ObjectTexture", 2D) = "white" {}
        _NoiseTex ("NoiseTexture", 2D) = "gray" {}

        [HideInInspector] _NoiseTexOffset0 ("NoiseTextureOffset0", Float) = 0
        [HideInInspector] _NoiseTexOffset1 ("NoiseTextureOffset1", Float) = 0

        [HideInInspector] _NoiseTexSize ("NoiseTextureSize", Float) = 64.0
        [HideInInspector] _ScreenRes_Width ("ScreenResWidth", Float) = 640.0
        [HideInInspector] _ScreenRes_Height ("ScreenResHeight", Float) = 480.0
        [HideInInspector] _AspectRatio ("AspectRatio", Float) = 1.0
        [HideInInspector] _Vuforia_Aspect ("VuforiaAspect", Float) = 1.0
        //[HideInInspector] _TexScale ("TexScale", Vector) = (1.0, 1.0, 1.0, 1.0)

        [Toggle] _EnableNoise ("Enable Noise", Float) = 1.0
        [Toggle] _EnableEdgeAntiAliasing ("Enable Edge Antialiasing", Float) = 1.0
        [Toggle] _EnableAlphaMixing ("Enable Alpha Mixing", Float) = 1.0 // for translucent surfaces
        _MultiplyNoise ("Multiply Noise", Range(0, 100)) = 10.0
        _IntensityBias ("Intensity Bias", Range(0, 1)) = 0.5
        _TexelMagnification ("Texel Magnification", Range(0, 16)) = 1.0
        _AA_Weight ("AntiAliasing Weight", Range(0, 1.0)) = 1.0
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
            #pragma multi_compile IOSBUILD_OFF IOSBUILD_IPADAIR1 IOSBUILD_IPADAIR2
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			/*** MAGIC NUMBERS ***/
			#define IPADAIR1_TEXSCALE_X 1.595000
			#define IPADAIR1_TEXSCALE_Y 1.420000
			#define IPADAIR2_TEXSCALE_X 1.585000
			#define IPADAIR2_TEXSCALE_Y 1.420000

			#define UPSCALE_IPADAIR1_TEX_X(x) (x * IPADAIR1_TEXSCALE_X)
			#define UPSCALE_IPADAIR1_TEX_Y(y) (y * IPADAIR1_TEXSCALE_Y)
			#define UPSCALE_IPADAIR2_TEX_X(x) (x * IPADAIR2_TEXSCALE_X)
			#define UPSCALE_IPADAIR2_TEX_Y(y) (y * IPADAIR2_TEXSCALE_Y)

			#define E _EnableEdgeAntiAliasing
			#define W _ScreenRes_Width
			#define H _ScreenRes_Height
			#define inv(i) (1.0 - i)

			uniform sampler2D _MainTex;
			uniform sampler2D _ObjectTex;
			uniform sampler2D _NoiseTex;

			uniform float4 _NoiseTex_ST;
			uniform float _NoiseTexOffset0;
			uniform float _NoiseTexOffset1;
			uniform float _NoiseTexSize;
			uniform float _ScreenRes_Width;
			uniform float _ScreenRes_Height;
			uniform float _AspectRatio;
			uniform float _Vuforia_Aspect;

			// Debug
			uniform float _EnableNoise;
			uniform float _MultiplyNoise;
			uniform float _IntensityBias;
			uniform float _TexelMagnification;
			uniform float _EnableEdgeAntiAliasing;
			uniform float _EnableAlphaMixing;
			uniform float _AA_Weight;
			// uniform float4 _TexScale;

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv0 : TEXCOORD0;
				half2 uv1 : TEXCOORD1;
			};

			float aspectfix(float uvx) 
			{
				float aspect = _AspectRatio/_Vuforia_Aspect;
				float shift = lerp(0, 1.0/aspect, (1.0 - aspect) * 0.5);
				return lerp(0, 1.0/aspect, uvx) - shift;
			}

			v2f VERT(appdata_img v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv0 = v.texcoord;

				_NoiseTex_ST = float4(
					(W*_AspectRatio/_NoiseTexSize)/_TexelMagnification, 
					(H/_NoiseTexSize)/_TexelMagnification,
					_NoiseTexOffset0, _NoiseTexOffset1);
				o.uv1 = TRANSFORM_TEX(v.texcoord, _NoiseTex);
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				fixed4 vidcol = tex2D(_MainTex, i.uv0);
				fixed4 noisecol = tex2D(_NoiseTex, i.uv1);

				#if IOSBUILD_IPADAIR1
				i.uv0.x = UPSCALE_IPADAIR1_TEX_X(i.uv0.x);
				i.uv0.y = UPSCALE_IPADAIR1_TEX_Y(i.uv0.y);
				#endif

				#if IOSBUILD_IPADAIR2
				i.uv0.x = UPSCALE_IPADAIR2_TEX_X(i.uv0.x);
				i.uv0.y = UPSCALE_IPADAIR2_TEX_Y(i.uv0.y);
				#endif

				// i.uv.x *= _TexScale.x;
				// i.uv.y *= _TexScale.y;

				fixed4 objcol = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)));
				fixed intensity = lerp(_IntensityBias, 1.0, (objcol.r + objcol.g + objcol.b)/3);

				objcol.a = round(objcol.a) * (inv(_EnableAlphaMixing)) + objcol.a * _EnableAlphaMixing;
				noisecol = (noisecol - fixed4(0.5)) * (inv(intensity));
				objcol.rgb = objcol.rgb + noisecol.rgb * objcol.a * _MultiplyNoise * _EnableNoise;

				// SIMPLE EDGE BLUR (4 texture hor/ver fetches)
				fixed4 p12 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(0, -1.0/H));		
				fixed4 p21 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(-1.0/W, 0));
				fixed4 p23 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(1.0/W, 0));		
				fixed4 p32 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(0, 1.0/H));

				fixed4 p12x = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(0, -2.0/H));		
				fixed4 p21x = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(-2.0/W, 0));
				fixed4 p23x = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(2.0/W, 0));		
				fixed4 p32x = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), inv(i.uv0.y)) + half2(0, 2.0/H));

				// SIMPLE EDGE BLUR (4 texture diagonal fetches)
				// fixed p11 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), 1.0 - i.uv0.y) + half2(-1.0/W, -1.0/H)).a;		
				// fixed p13 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), 1.0 - i.uv0.y) + half2(1.0/W, -1.0/H)).a;
				// fixed p31 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), 1.0 - i.uv0.y) + half2(-1.0/W, 1.0/H)).a;		
				// fixed p33 = tex2D(_ObjectTex, half2(aspectfix(i.uv0.x), 1.0 - i.uv0.y) + half2(1.0/W, 1.0/H)).a;
					
				fixed4 avgcol = p12 * p12.a + p21 * p21.a + p23 * p23.a + p32 * p32.a; //+ p12x + p21x + p23x + p32x;
				fixed weight = (int)p12.a + (int)p21.a + (int)p23.a + (int)p32.a; // + p12x.a + p21x.a + p23x.a + p32x.a;

				if (weight == 0.0) // pixel is video
					return vidcol;

				if (weight == 4.0) // pixel is obj
					return objcol;

				weight /= 4.0;
				//avgcol *= (weight/4.0);

				//objcol.rgb = objcol.rgb * objcol.a + ((objcol + avgcol)/2.0) * inv(objcol.a);

				// if (weight == 0)
				// 	objcol.rgb = vidcol.rgb;
				// else if (weight > 0)
				// 	objcol.rgb = objcol.rgb;
				// else
				// 	objcol.rgb = inccol.rgb / weight; // * inv(objcol.a) + objcol.rgb * objcol.a;

				//weight = max(weight * (_AA_Weight * (inv(objcol.a))), objcol.a);

				fixed3 rescol = fixed3(lerp(vidcol.rgb, avgcol.rgb, 0.25)); /*weight * E + objcol.a * inv(E)));*/
				return fixed4(rescol.rgb, 1.0);
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}
