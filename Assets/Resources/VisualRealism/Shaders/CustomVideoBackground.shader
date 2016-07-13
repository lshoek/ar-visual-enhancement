Shader "Custom/CustomVideoBackground" 
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

        [HideInInspector] _ENABLE_NOISE ("Enable Noise", Float) = 1.0
        [HideInInspector] _ENABLE_ALPHA_MIXING ("Enable Alpha Mixing", Float) = 1.0 // for translucent surfaces
        [HideInInspector] _MULTIPLY_NOISE ("Multiply Noise", Range(0, 100.0)) = 10.0
        [HideInInspector] _INTENSITY_BIAS ("Intensity Bias", Range(0, 1.0)) = 0.5
        [HideInInspector] _TEXEL_MAGNIFICATION ("Texel Magnification", Range(0, 16.0)) = 1.0
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
			#define IPADAIR1_TEXSCALE_X 	1.595000
			#define IPADAIR1_TEXSCALE_Y 	1.420000
			#define IPADAIR2_TEXSCALE_X 	1.585000
			#define IPADAIR2_TEXSCALE_Y 	1.420000
			#define IPADAIRPRO_TEXSCALE_X 	1.600000
			#define IPADAIRPRO_TEXSCALE_Y 	1.067000

			#define UPSCALE_IPADAIR1_TEX_X(x) (x * IPADAIR1_TEXSCALE_X)
			#define UPSCALE_IPADAIR1_TEX_Y(y) (y * IPADAIR1_TEXSCALE_Y)
			#define UPSCALE_IPADAIR2_TEX_X(x) (x * IPADAIR2_TEXSCALE_X)
			#define UPSCALE_IPADAIR2_TEX_Y(y) (y * IPADAIR2_TEXSCALE_Y)
			#define UPSCALE_IPADAIRPRO_TEX_X(x) (x * IPADAIRPRO_TEXSCALE_X)
			#define UPSCALE_IPADAIRPRO_TEX_Y(y) (y * IPADAIRPRO_TEXSCALE_Y)

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
			uniform float _ENABLE_NOISE;
			uniform float _ENABLE_ALPHA_MIXING;
			uniform float _MULTIPLY_NOISE;
			uniform float _INTENSITY_BIAS;
			uniform float _TEXEL_MAGNIFICATION;
			// uniform float4 _TexScale;

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv0 : TEXCOORD0;
				half2 uv1 : TEXCOORD1;
				half2 uv2 : TEXCOORD2;
			};

			half2 aspectfix(half2 uv) 
			{
				float aspect = _AspectRatio/_Vuforia_Aspect;
				float shift = lerp(0, 1.0/aspect, (1.0 - aspect) * 0.5);
				return half2(lerp(0, 1.0/aspect, uv.x) - shift, inv(uv.y));
			}

			v2f VERT(appdata_img v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
 
				#if IOSBUILD_IPADAIR1
				o.uv2.x = UPSCALE_IPADAIR1_TEX_X(v.texcoord.x);
				o.uv2.y = UPSCALE_IPADAIR1_TEX_Y(v.texcoord.y);
				#elif IOSBUILD_IPADAIR2
				o.uv2.x = UPSCALE_IPADAIR2_TEX_X(v.texcoord.x);
				o.uv2.y = UPSCALE_IPADAIR2_TEX_Y(v.texcoord.y);
				#elif IOSBUILD_IPADAIRPRO
				i.uv.x = UPSCALE_IPADAIRPRO_TEX_X(v.texcoord.x);
				i.uv.y = UPSCALE_IPADAIRPRO_TEX_Y(v.texcoord.y);
				#else
				o.uv2 = v.texcoord;
				#endif

				// i.uv.x *= _TexScale.x;
				// i.uv.y *= _TexScale.y;

				_NoiseTex_ST = half4(
					(W*_AspectRatio/_NoiseTexSize)/_TEXEL_MAGNIFICATION, 
					(H/_NoiseTexSize)/_TEXEL_MAGNIFICATION,
					_NoiseTexOffset0, _NoiseTexOffset1);

				o.uv0 = v.texcoord;
				o.uv1 = TRANSFORM_TEX(v.texcoord, _NoiseTex);
				o.uv2 = aspectfix(o.uv2);
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				fixed4 vidcol = tex2D(_MainTex, i.uv0);
				fixed4 noisecol = tex2D(_NoiseTex, i.uv1);
				fixed4 objcol = tex2D(_ObjectTex, i.uv2);
				fixed intensity = lerp(_INTENSITY_BIAS, 1.0, (objcol.r + objcol.g + objcol.b)/3.0);

				objcol.a = objcol.a * (inv(_ENABLE_ALPHA_MIXING)) + objcol.a * _ENABLE_ALPHA_MIXING;
				noisecol = (noisecol - fixed4(0.5, 0.5, 0.5, 0.5)) * (inv(intensity));
				objcol.rgb = objcol.rgb + noisecol.rgb * objcol.a * _MULTIPLY_NOISE * _ENABLE_NOISE;

				fixed3 rescol = fixed3(lerp(vidcol.rgb, objcol.rgb, objcol.a));
				return fixed4(rescol.rgb, 1.0);
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}
