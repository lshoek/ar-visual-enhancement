Shader "Custom/Benchmark"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }

		CGPROGRAM
		#pragma surface SURF Standard vertex:VERT nofog keepalpha
		#pragma debug
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input 
		{
			fixed3 vertexColor;
			float2 uv_MainTex;
		};

		void VERT (inout appdata_base v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vertexColor = abs(v.normal);
		}

		void SURF (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb * IN.vertexColor;
			o.Alpha = c.a;
		}
		ENDCG
	}
}
