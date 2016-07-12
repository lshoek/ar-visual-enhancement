Shader "Custom/RimLight" 
{
	Properties 
	{
	  _MainTex ("Texture", 2D) = "white" {}
	  _Color ("Color", Color) = (1.0, 0.0, 0.0, 1.0)
	  _RimColor ("Rim Color", Color) = (0.26, 0.19, 0.16, 0.0)
	  _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
	}

	SubShader 
	{
		Tags { "RenderType" = "Transparent" "Queue"="Transparent" }

		CGPROGRAM
		#pragma surface surf Standard vertex:vert keepalpha alpha:_CutOff
		#include "UnityCG.cginc"

		struct appdata 
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 tangent : TANGENT;
		};

		struct Input 
		{
			float2 uv_MainTex;
			float3 normal;
			float3 viewDir;
		};

		sampler2D _MainTex;
		fixed4 _Color;
		fixed4 _RimColor;
		fixed _RimPower;

		void vert(inout appdata v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.normal = v.normal;
		}

		void surf(Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = col.rgb;
			o.Alpha = col.a;
			o.Normal = IN.normal;

			fixed rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
			o.Emission = _RimColor.rgb * pow (rim, _RimPower);
		}
		ENDCG
	} 
}
