Shader "Custom/GenerateNoiseTexture"
{
    Properties 
    {
		_MainTex ("Tex_Input", 2D) = "white" {}
		_Tex_Size ("Tex_Size", Float) = 256.0
		_Mean_R ("Mean_R", Float) = 1.0
		_Mean_G ("Mean_G", Float) = 1.0
		_Mean_B ("Mean_B", Float) = 1.0
		_SD_R ("SD_R", Float) = 1.0
		_SD_G ("SD_G", Float) = 1.0
		_SD_B ("SD_B", Float) = 1.0
    }
   
    SubShader 
    {   
        ZWrite On
        Cull Off

        Pass 
        {
			GLSLPROGRAM

			#define PI 3.14159265359
			#define EXP 2.71828182845

			uniform sampler2D _MainTex;
			uniform float _Tex_Size;

			uniform float _Mean_R;
			uniform float _Mean_G;
			uniform float _Mean_B;
			uniform float _SD_R;
			uniform float _SD_G;
			uniform float _SD_B;

			const vec2 k = vec2(23.1406926327792690,2.6651441426902251);
			varying vec2 t;

			// Michaelangel007 // http://stackoverflow.com/questions/5149544/can-i-generate-a-random-number-inside-a-pixel-shader
			float rnd0(vec2 uv) { return fract(cos(mod(123456.0, 1024.0 * dot(uv,k)))); }
			float rnd1(vec2 uv) { return fract(cos(mod(1234567.0, 1024.0 * dot(uv,k)))); }
			float rnd2(vec2 uv) { return fract(cos(mod(12345678.0, 1024.0 * dot(uv,k)))); }
           
            #ifdef VERTEX
            void main()
            {
				gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				t = gl_MultiTexCoord0.xy;
            }
            #endif
           
            #ifdef FRAGMENT
            void main()
            {
    			float gauss0 = 1.0/(_SD_R * sqrt(2.0*PI)) * EXP * -(pow(rnd0(t)-_Mean_R, 2.0)/pow(2.0*_SD_R, 2.0));
				float gauss1 = 1.0/(_SD_G * sqrt(2.0*PI)) * EXP * -(pow(rnd1(t)-_Mean_G, 2.0)/pow(2.0*_SD_G, 2.0));
				float gauss2 = 1.0/(_SD_B * sqrt(2.0*PI)) * EXP * -(pow(rnd2(t)-_Mean_B, 2.0)/pow(2.0*_SD_B, 2.0));

				gl_FragColor = vec4(0.5) + vec4(gauss0, gauss1, gauss2, 1.0);
				gl_FragColor.a = 1.0;
            }
			#endif

            ENDGLSL
        }
    }
}

