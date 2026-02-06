Shader "Custom/PixelArtEdgeDetectionDebug"
{
    Properties
    {
        _DebugMode ("Debug Mode", Range(0, 5)) = 0
        _DepthThreshold ("Depth Threshold", Range(0, 5)) = 1.5
        _EdgeThickness ("Edge Thickness", Range(0.5, 3)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "Edge Detection Debug"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            float _DebugMode;
            float _DepthThreshold;
            float _EdgeThickness;
            
            half4 frag(Varyings input) : SV_Target
            {
                int debugMode = (int)_DebugMode;
                float2 uv = input.texcoord;
                
                // MODE 0: Color sólido MAGENTA - verificar que el shader se ejecuta
                if (debugMode == 0)
                {
                    return half4(1, 0, 1, 1); // MAGENTA
                }
                
                // MODE 1: Mostrar UVs como colores - verificar coordenadas
                if (debugMode == 1)
                {
                    return half4(uv.x, uv.y, 0, 1);
                }
                
                // MODE 2: Mostrar la imagen original (_BlitTexture viene del Blitter)
                if (debugMode == 2)
                {
                    half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                    return color;
                }
                
                // MODE 3: Verificar TexelSize
                if (debugMode == 3)
                {
                    return half4(_BlitTexture_TexelSize.xy * 1000, 0, 1);
                }
                
                // MODE 4: Visualizar depth texture (usar raw depth, mejor para visualización)
                if (debugMode == 4)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    // En Unity, rawDepth está en [0,1] donde 0 = cerca, 1 = lejos (reversed-Z)
                    // Invertir para visualizar mejor: objetos cercanos = blanco, lejanos = negro
                    float visualDepth = 1.0 - rawDepth;
                    return half4(visualDepth, visualDepth, visualDepth, 1);
                }
                
                // MODE 5: Edge detection completo
                if (debugMode == 5)
                {
                    half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                    
                    float2 texelSize = _BlitTexture_TexelSize.xy;
                    
                    // Usar raw depth (sin linearizar) - mejor para edge detection
                    float depthCenter = SampleSceneDepth(uv);
                    float depthRight = SampleSceneDepth(uv + float2(texelSize.x, 0) * _EdgeThickness);
                    float depthUp = SampleSceneDepth(uv + float2(0, texelSize.y) * _EdgeThickness);
                    float depthLeft = SampleSceneDepth(uv + float2(-texelSize.x, 0) * _EdgeThickness);
                    float depthDown = SampleSceneDepth(uv + float2(0, -texelSize.y) * _EdgeThickness);
                    
                    // Sobel-like edge detection
                    float diffX = abs(depthLeft - depthRight);
                    float diffY = abs(depthUp - depthDown);
                    
                    // Normalizar por el depth del centro para hacer el threshold relativo
                    // Esto evita que objetos lejanos tengan edges más débiles
                    float depthNormalized = max(depthCenter, 0.0001);
                    float edge = (diffX + diffY) / depthNormalized;
                    
                    // Aplicar threshold - valores más bajos = más edges
                    edge = saturate(step(_DepthThreshold * 0.01, edge));
                    
                    return lerp(baseColor, half4(0, 0, 0, 1), edge);
                }
                
                // Fallback
                return half4(1, 1, 0, 1); // AMARILLO = modo desconocido
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}