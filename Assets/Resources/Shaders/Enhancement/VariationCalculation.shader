Shader "Custom/VariationCalculation"
{
	Properties 
    {
		_Texture_Ref_Frame ("ReferenceFrame", 2D) = "white" {}
		_Texture_Avg_Frame ("AverageFrame", 2D) = "white" {}
    }
   
    SubShader 
    {   
        ZWrite On
        Cull Off

        Pass 
        {
			GLSLPROGRAM
			uniform sampler2D _Texture_Ref_Frame;
			uniform sampler2D _Texture_Avg_Frame;
			varying vec2 texCoordinate0;
            const float MUL_NOISE = 3.0;
           
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
            	vec4 ref_col = texture2D (_Texture_Ref_Frame, texCoordinate0);
            	vec4 avg_col = texture2D (_Texture_Avg_Frame, texCoordinate0);
				gl_FragColor = vec4(0.5) + vec4(ref_col - avg_col) * vec4(MUL_NOISE);
                gl_FragColor.a = 1.0;
            }
            #endif
            ENDGLSL
        }
    }
}
