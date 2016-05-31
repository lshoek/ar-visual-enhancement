Shader "Custom/PPFXNoise"
{
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_ColorX ("ColorX", Color) = (1.0, 1.0, 1.0, 1.0)
		_ColorY ("ColorY", Color) = (0.0, 0.0, 0.0, 1.0)
		[HideInInspector] _ElapsedTime ("ElapsedTime", Float) = 1.0
	}
	
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex VERT
			#pragma fragment FRAG
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed3 _ColorX;
			fixed3 _ColorY;
			fixed _ElapsedTime;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			// SIMPLENOISE BEGIN
			float3 mod289(float3 x) {
				return x - floor(x * (1.0 / 289.0)) * 289.0;
			}

			float2 mod289(float2 x) {
				return x - floor(x * (1.0 / 289.0)) * 289.0;
			}

			float3 permute(float3 x) {
				return mod289(((x*34.0)+1.0)*x);
			}

			fixed snoise(fixed2 v)
			{
				const fixed4 C = fixed4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
				fixed2 i  = floor(v + dot(v, C.yy));
				fixed2 x0 = v -   i + dot(i, C.xx);

				fixed2 i1;
				i1 = (x0.x > x0.y) ? fixed2(1.0, 0.0) : fixed2(0.0, 1.0);
				fixed4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;

				i = mod289(i);
				fixed3 p = permute( permute( i.y + fixed3(0.0, i1.y, 1.0 )) + i.x + fixed3(0.0, i1.x, 1.0 ));

				fixed3 m = max(0.5 - fixed3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
				m = m*m ;
				m = m*m ;

				fixed3 x = 2.0 * frac(p * C.www) - 1.0;
				fixed3 h = abs(x) - 0.5;
				fixed3 ox = floor(x + 0.5);
				fixed3 a0 = x - ox;

				m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);

				fixed3 g;
				g.x  = a0.x  * x0.x  + h.x  * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot(m, g);
			}
			// SIMPLENOISE END

			v2f VERT (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 FRAG (v2f i) : SV_Target
			{
				// play with texture coordinates
				float2 txcoord = i.uv;
				txcoord.x += sin(50.0 * txcoord.y + _ElapsedTime) / 100;
				
				// calculate noise color
				fixed4 txcol = tex2D(_MainTex, txcoord);
				fixed coeff = 0.2f;
				fixed fbm = 
					(2.0*sin(_ElapsedTime))*snoise(fixed2(5.0*txcoord*coeff))
					+ 0.5*snoise(fixed2(10.0*txcoord*coeff))
					+ 0.15*snoise(fixed2(20.0*txcoord*coeff))
					+ 0.125*snoise(fixed2(40.0*txcoord*coeff))
					+ 0.0625*snoise(fixed2(80.0*txcoord*coeff));

				// modify greyscale noise color to a custom rgb color
				fixed3 mxcol = fixed3(clamp(_ColorX, _ColorY, fbm));
				
				// calculate mixed color (noise layer|object layer)
				fixed4 outp = fixed4(txcol.rgb * txcol.a + abs(1-txcol.a) * mxcol, 1.0);

				return outp;
			}
			ENDCG
		}
	}
}
