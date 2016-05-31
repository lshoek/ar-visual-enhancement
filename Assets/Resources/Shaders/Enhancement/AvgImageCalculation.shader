Shader "Custom/AvgImageCalculation" 
{
    Properties 
    {
		_Texture_Frame_0 ("TextureFrame0", 2D) = "white" {}
		_Texture_Frame_1 ("TextureFrame1", 2D) = "white" {}
		_Texture_Frame_2 ("TextureFrame2", 2D) = "white" {}
        _Texture_Frame_3 ("TextureFrame3", 2D) = "white" {}
        _NumRefFrames ("NumRefFrames", Float) = 4.0
    }
   
    SubShader 
    {   
        ZWrite On
        Cull Off

        Pass 
        {
			GLSLPROGRAM
			uniform sampler2D _Texture_Frame_0;
			uniform sampler2D _Texture_Frame_1;
			uniform sampler2D _Texture_Frame_2;
            uniform sampler2D _Texture_Frame_3;
            uniform float _NumRefFrames;
			varying vec2 texCoordinate0;
           
            #ifdef VERTEX
            void main()
            {
            	texCoordinate0 = gl_MultiTexCoord0.xy;
				gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
            }
            #endif
           
            #ifdef FRAGMENT
            void main()
            {
            	//vec2 fixedTexCoord = vec2(texCoordinate0.x, 1.0 - texCoordinate0.y);
            	vec4 col = texture2D (_Texture_Frame_0, texCoordinate0);
                col += texture2D (_Texture_Frame_1, texCoordinate0);
				col += texture2D (_Texture_Frame_2, texCoordinate0);
                col += texture2D (_Texture_Frame_3, texCoordinate0);
				col /= _NumRefFrames;

				gl_FragColor = vec4(col.rgb, 1.0);
            }
            #endif
            ENDGLSL
        }
    }
}
