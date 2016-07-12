Shader "Custom/VertexSineWave" 											// the name of our shader, usually "Custom/[NAME]"
{
	Properties															// define what variables to use in this shader
	{
		_Color ("Color (RGBA)", Color) = (1.0, 1.0, 1.0, 1.0)			// color tint (rgba)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}						// texture image/object, if there's no texture we use the color white by default 
		[HideInInspector] _ElapsedTime ("ElapsedTime", Float) = 1.0		// user-defined property ElapsedTime (modified every frame from shadercontroller script)
	}

	SubShader 															// opening keyword for our (sub)shader
	{
		Tags 															// subshader tags to configure when our object is drawn
		{
			"Queue" = "Transparent" 									// tell unity to render after Geometry
		}

		CGPROGRAM														// opening keyword for our cg code
		#pragma surface SURF Standard vertex:VERT nofog					// pragma keywords to configure how unity precompiles our shader
		#include "UnityCG.cginc"										// we need this to get access to the UNITY_INITIALIZE_OUTPUT func

		struct Input {													// input struct, used to write vertex info to surface func
			fixed3 vertexColor;											// user-defined vertex color variable, low precision
			float2 uv_MainTex;											// built-in uv coordinates variable
		};
		sampler2D _MainTex;												// variable to access the main texture object
		fixed4 _Color;													// color tint property
		half _ElapsedTime;												// low precision variable to store elapsed time

		void VERT (inout appdata_base v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);							// init output struct. only when modifying values
			v.vertex.x += sin(5.0 * v.vertex.y + _ElapsedTime) * 0.3;	// change local z-position based on the sine of its y
			o.vertexColor = abs(v.normal);								// write normal as color to input struct to use in surface func
		}

		void SURF (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;		// get the color value from _MainTex
			o.Albedo = c.rgb * IN.vertexColor;							// multiply the texture and vertex color
			o.Alpha = c.a;												// write the alpha value from the texture to separate alpha output (not necessary here)
		}
		ENDCG															// closing keyword for our cg code
	}
	FallBack "Diffuse"													// fallback to diffuse in case the shader fails
}
