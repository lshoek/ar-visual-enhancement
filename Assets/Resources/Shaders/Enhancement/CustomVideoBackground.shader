Shader "Custom/CustomVideoBackground" 
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ObjectTex ("ObjectTexture", 2D) = "white" {}
        _NoiseTex ("NoiseTexture", 2D) = "gray" {}

        _NoiseTexOffset0 ("NoiseTextureOffset0", Float) = 0
        _NoiseTexOffset1 ("NoiseTextureOffset1", Float) = 0
        
        _NoiseTexSize ("NoiseTextureSize", Float) = 64.0
        _ScreenRes_Width ("ScreenResWidth", Float) = 640.0
        _ScreenRes_Height ("ScreenResHeight", Float) = 480.0
        _AspectRatio ("AspectRatio", Float) = 1.0
        _Vuforia_Aspect ("VuforiaAspect", Float) = 1.0
        //_TexScale ("TexScale", Vector) = (1.0, 1.0, 1.0, 1.0)

        [Toggle] _EnableNoise ("EnableNoise", Float) = 0
        _TweakNoise ("TweakNoise", Range(0, 10)) = 1.0
        [Toggle] _DisableAlphaMixing ("DisableAlphaMixing", Float) = 0
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
			uniform float _TweakNoise;
			uniform float _DisableAlphaMixing;

			// uniform float4 _TexScale;

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
				half2 uv2 : TEXCOORD2;
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
				o.uv = v.texcoord;

				_NoiseTex_ST = float4(
					_ScreenRes_Width/_NoiseTexSize, _ScreenRes_Height/_NoiseTexSize,
					_NoiseTexOffset0, _NoiseTexOffset1);
				o.uv2 = TRANSFORM_TEX(v.texcoord, _NoiseTex);
				return o;
			}

			fixed4 FRAG (v2f i) : COLOR
			{
				fixed4 vidcol = tex2D(_MainTex, i.uv);
				fixed4 noisecol = tex2D(_NoiseTex, i.uv2);

				#if IOSBUILD_IPADAIR1
				i.uv.x = UPSCALE_IPADAIR1_TEX_X(i.uv.x);
				i.uv.y = UPSCALE_IPADAIR1_TEX_Y(i.uv.y);
				#endif

				#if IOSBUILD_IPADAIR2
				i.uv.x = UPSCALE_IPADAIR2_TEX_X(i.uv.x);
				i.uv.y = UPSCALE_IPADAIR2_TEX_Y(i.uv.y);
				#endif

				// i.uv.x *= _TexScale.x;
				// i.uv.y *= _TexScale.y;

				fixed4 objcol = tex2D(_ObjectTex, float2(aspectfix(i.uv.x), 1.0 - i.uv.y));
				fixed intensity = clamp(((objcol.r + objcol.g + objcol.b)/3), 0, 1.0);

				objcol.a = round(objcol.a) * _DisableAlphaMixing + objcol.a * (1.0 - _DisableAlphaMixing);
				noisecol = (noisecol - fixed4(0.5)) * (1.0 - intensity);
				objcol.rgb = objcol.rgb + noisecol.rgb * objcol.a * _TweakNoise * _EnableNoise;
				
				fixed3 fcol = fixed3(lerp(vidcol.rgb, objcol.rgb, objcol.a));
				return fixed4(fcol.rgb, 1.0);
			}
			ENDCG
        }
    } 
    FallBack "Diffuse"
}
