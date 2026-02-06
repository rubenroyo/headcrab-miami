Shader "Custom/PixelArtEdgeDetectionDebug"
{
    Properties
    {
        _DebugMode ("Debug Mode", Range(0, 7)) = 0
        _DepthThreshold ("Depth Threshold", Range(0, 5)) = 1.5
        _EdgeThickness ("Edge Thickness", Range(0.5, 3)) = 1
        _DepthNear ("Depth Near", Range(0, 50)) = 8
        _DepthFar ("Depth Far", Range(1, 100)) = 15
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
            float _DepthNear;
            float _DepthFar;
            
            // Función para calcular edge basado en depth
            // Solo pinta como edge el pixel MÁS CERCANO (menor depth)
            float CalculateDepthEdge(float2 uv, float2 texelSize)
            {
                float depthCenter = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float depthRight = LinearEyeDepth(SampleSceneDepth(uv + float2(texelSize.x, 0)), _ZBufferParams);
                float depthUp = LinearEyeDepth(SampleSceneDepth(uv + float2(0, texelSize.y)), _ZBufferParams);
                float depthLeft = LinearEyeDepth(SampleSceneDepth(uv + float2(-texelSize.x, 0)), _ZBufferParams);
                float depthDown = LinearEyeDepth(SampleSceneDepth(uv + float2(0, -texelSize.y)), _ZBufferParams);
                
                // Calcular diferencia: positivo = vecino más lejos, negativo = vecino más cerca
                float diffRight = depthRight - depthCenter;
                float diffLeft = depthLeft - depthCenter;
                float diffUp = depthUp - depthCenter;
                float diffDown = depthDown - depthCenter;
                
                // Es edge SOLO si hay un vecino MÁS LEJOS con diferencia >= threshold
                // (esto significa que el pixel actual es el más cercano, así que "recibe" el borde)
                float edge = 0;
                if (diffRight >= _DepthThreshold) edge = 1;
                if (diffLeft >= _DepthThreshold) edge = 1;
                if (diffUp >= _DepthThreshold) edge = 1;
                if (diffDown >= _DepthThreshold) edge = 1;
                
                return edge;
            }
            
            // Función para visualizar depth con contraste ajustable
            float VisualizeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                // Mapear: _DepthNear = negro (0), _DepthFar = blanco (1)
                float visualDepth = saturate((linearDepth - _DepthNear) / (_DepthFar - _DepthNear));
                return visualDepth;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                int debugMode = (int)_DebugMode;
                float2 uv = input.texcoord;
                float2 texelSize = _BlitTexture_TexelSize.xy * _EdgeThickness;
                
                // MODE 0: Color sólido MAGENTA - verificar que el shader se ejecuta
                if (debugMode == 0)
                {
                    return half4(1, 0, 1, 1);
                }
                
                // MODE 1: Mostrar UVs como colores
                if (debugMode == 1)
                {
                    return half4(uv.x, uv.y, 0, 1);
                }
                
                // MODE 2: Mostrar la imagen original
                if (debugMode == 2)
                {
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                }
                
                // MODE 3: Raw depth (sin procesar, para debug)
                if (debugMode == 3)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    return half4(rawDepth, rawDepth, rawDepth, 1);
                }
                
                // MODE 4: Depth map visualizado (usa _DepthNear y _DepthFar para contraste)
                if (debugMode == 4)
                {
                    float visualDepth = VisualizeDepth(uv);
                    return half4(visualDepth, visualDepth, visualDepth, 1);
                }
                
                // MODE 5: Solo edges exteriores (ROJO sobre negro)
                if (debugMode == 5)
                {
                    float edge = CalculateDepthEdge(uv, texelSize);
                    return half4(edge, 0, 0, 1);
                }
                
                // MODE 6: Depth map + edges superpuestos (edges en rojo)
                if (debugMode == 6)
                {
                    float visualDepth = VisualizeDepth(uv);
                    float edge = CalculateDepthEdge(uv, texelSize);
                    
                    half3 depthColor = half3(visualDepth, visualDepth, visualDepth);
                    half3 edgeColor = half3(1, 0, 0);
                    
                    return half4(lerp(depthColor, edgeColor, edge), 1);
                }
                
                // MODE 7: Resultado final (imagen + edges con Darken)
                if (debugMode == 7)
                {
                    half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                    float edge = CalculateDepthEdge(uv, texelSize);
                    
                    // Blending tipo Darken/Multiply
                    float darkenAmount = edge * 0.85;
                    half3 finalColor = baseColor.rgb * (1.0 - darkenAmount);
                    
                    return half4(finalColor, baseColor.a);
                }
                
                // Fallback
                return half4(1, 1, 0, 1);
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}