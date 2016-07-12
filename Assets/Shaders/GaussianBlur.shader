Shader "Custom/GaussianBlur"
{
    // Incremental Gaussian Coefficent Calculation (See GPU Gems 3 pp. 877 - 889)
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Sigma ("Sigma", Float) = 5.0
    }

    // _Sigma
        // 9x9 : 3 to 5
        // 7x7 : 2.5 to 4
        // 5x5 : 2 to 3.5

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass 
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment FRAG
            #include "UnityCG.cginc"

            #define PI 3.14159265
            #define HORIZONTAL_BLUR_9

            #if defined(VERTICAL_BLUR_9)
            const float numBlurPixelsPerSide = 4.0f;
            const float2 blurMultiplyVec = float2(0.0f, 1.0f);

            #elif defined(HORIZONTAL_BLUR_9)
            const float numBlurPixelsPerSide = 4.0f;
            const float2 blurMultiplyVec = float2(1.0f, 0.0f);

            #elif defined(VERTICAL_BLUR_7)
            const float numBlurPixelsPerSide = 3.0f;
            const float2 blurMultiplyVec = float2(0.0f, 1.0f);

            #elif defined(HORIZONTAL_BLUR_7)
            const float numBlurPixelsPerSide = 3.0f;
            const float2 blurMultiplyVec = float2(1.0f, 0.0f);

            #elif defined(VERTICAL_BLUR_5)
            const float numBlurPixelsPerSide = 2.0f;
            const float2 blurMultiplyVec = float2(0.0f, 1.0f);

            #elif defined(HORIZONTAL_BLUR_5)
            const float numBlurPixelsPerSide = 2.0f;
            const float2 blurMultiplyVec = float2(1.0f, 0.0f);

            #else
            const float numBlurPixelsPerSide = 0.0f;
            const float2 blurMultiplyVec = float2(0.0f, 0.0f);
            #endif

            uniform sampler2D _MainTex;
            uniform float _Sigma;
            uniform float _Horizontal;
            uniform float _BlurSize;

            fixed4 FRAG(v2f_img img) : SV_TARGET
            {  
                float3 incrGaussian;
                incrGaussian.x = 1.0 / (sqrt(2.0 * PI) * _Sigma);
                incrGaussian.y = exp(-0.5 / (_Sigma * _Sigma));
                incrGaussian.z = incrGaussian.y * incrGaussian.y;

                float4 avgValue = float4(0.0, 0.0, 0.0, 0.0);
                float coefficientSum = 0.0;

                // central sample
                avgValue += tex2D(_MainTex, img.uv) * incrGaussian.x;
                coefficientSum += incrGaussian.x;
                incrGaussian.xy *= incrGaussian.yz;

                // remaining samples
                for (float i = 1.0; i <= numBlurPixelsPerSide; i++) 
                { 
                    avgValue += tex2D(_MainTex, img.uv - i * _BlurSize * blurMultiplyVec) * incrGaussian.x;         
                    avgValue += tex2D(_MainTex, img.uv + i * _BlurSize * blurMultiplyVec) * incrGaussian.x;         
                    coefficientSum += 2.0 * incrGaussian.x;
                    incrGaussian.xy *= incrGaussian.yz;
                }
                
                return avgValue / coefficientSum;
            }
            ENDCG
        }
    }
}