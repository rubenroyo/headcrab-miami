Shader "Custom/PixelArtEdgeDetectionDebug"
{
    Properties
    {
        _DebugMode ("Debug Mode", Range(0, 8)) = 0
        _DepthThreshold ("Depth Threshold", Range(0, 5)) = 1.5
        _NormalThreshold ("Normal Threshold", Range(0, 2)) = 0.5
        _EdgeThickness ("Edge Thickness", Range(0.5, 3)) = 1
        _DepthNear ("Depth Near", Range(0, 50)) = 8
        _DepthFar ("Depth Far", Range(1, 100)) = 15
        _DarkenAmount ("Darken Amount", Range(0, 1)) = 0.85
        _BrightenAmount ("Brighten Amount", Range(0, 1)) = 0.3
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            float _DebugMode;
            float _DepthThreshold;
            float _NormalThreshold;
            float _EdgeThickness;
            float _DepthNear;
            float _DepthFar;
            float _DarkenAmount;
            float _BrightenAmount;
            
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
                float visualDepth = saturate((linearDepth - _DepthNear) / (_DepthFar - _DepthNear));
                return visualDepth;
            }
            
            // Clasifica una normal en una de las 6 caras (0=X+, 1=Y+, 2=Z+, 3=X-, 4=Y-, 5=Z-)
            int ClassifyNormal(float3 n)
            {
                float3 absN = abs(n);
                
                // Encontrar el eje dominante
                if (absN.x >= absN.y && absN.x >= absN.z)
                    return n.x >= 0 ? 0 : 3; // X+ o X-
                else if (absN.y >= absN.x && absN.y >= absN.z)
                    return n.y >= 0 ? 1 : 4; // Y+ o Y-
                else
                    return n.z >= 0 ? 2 : 5; // Z+ o Z-
            }
            
            // Tabla de prioridad binaria: 1 = pintar edge (pierdes), 0 = no pintar (ganas o imposible)
            // Reglas: X+ gana a Z+, Z+ gana a X-, X- gana a Z-, Z- gana a X+
            // Y+ e Y- ganan a todos
            // Filas = pixel centro, Columnas = pixel vecino
            //        X+  Y+  Z+  X-  Y-  Z-
            // X+   [ 0,  1,  0,  0,  1,  1 ]
            // Y+   [ 0,  0,  0,  0,  0,  0 ]
            // Z+   [ 1,  1,  0,  0,  1,  0 ]
            // X-   [ 0,  1,  1,  0,  1,  0 ]
            // Y-   [ 0,  0,  0,  0,  0,  0 ]
            // Z-   [ 0,  1,  0,  1,  1,  0 ]
            int GetPriority(int centerFace, int neighborFace)
            {
                if (centerFace == 0) // X+: pierde contra Y+, Y-, Z-
                {
                    if (neighborFace == 1) return 1; // Y+ gana
                    if (neighborFace == 4) return 1; // Y- gana
                    if (neighborFace == 5) return 1; // Z- gana
                    return 0;
                }
                if (centerFace == 1) // Y+: gana a todos
                {
                    return 0;
                }
                if (centerFace == 2) // Z+: pierde contra X+, Y+, Y-
                {
                    if (neighborFace == 0) return 1; // X+ gana
                    if (neighborFace == 1) return 1; // Y+ gana
                    if (neighborFace == 4) return 1; // Y- gana
                    return 0;
                }
                if (centerFace == 3) // X-: pierde contra Y+, Z+, Y-
                {
                    if (neighborFace == 1) return 1; // Y+ gana
                    if (neighborFace == 2) return 1; // Z+ gana
                    if (neighborFace == 4) return 1; // Y- gana
                    return 0;
                }
                if (centerFace == 4) // Y-: gana a todos
                {
                    return 0;
                }
                if (centerFace == 5) // Z-: pierde contra Y+, X-, Y-
                {
                    if (neighborFace == 1) return 1; // Y+ gana
                    if (neighborFace == 3) return 1; // X- gana
                    if (neighborFace == 4) return 1; // Y- gana
                    return 0;
                }
                return 0;
            }
            
            // Función para calcular edge basado en normales usando tabla de prioridad
            float CalculateNormalEdge(float2 uv, float2 texelSize)
            {
                float3 normalCenter = SampleSceneNormals(uv);
                float3 normalRight = SampleSceneNormals(uv + float2(texelSize.x, 0));
                float3 normalUp = SampleSceneNormals(uv + float2(0, texelSize.y));
                float3 normalLeft = SampleSceneNormals(uv + float2(-texelSize.x, 0));
                float3 normalDown = SampleSceneNormals(uv + float2(0, -texelSize.y));
                
                int faceCenter = ClassifyNormal(normalCenter);
                int faceRight = ClassifyNormal(normalRight);
                int faceUp = ClassifyNormal(normalUp);
                int faceLeft = ClassifyNormal(normalLeft);
                int faceDown = ClassifyNormal(normalDown);
                
                float edge = 0;
                
                // Check diferencia + tabla de prioridad
                float diffRight = 1.0 - dot(normalCenter, normalRight);
                if (diffRight >= _NormalThreshold && GetPriority(faceCenter, faceRight) == 1) edge = 1;
                
                float diffLeft = 1.0 - dot(normalCenter, normalLeft);
                if (diffLeft >= _NormalThreshold && GetPriority(faceCenter, faceLeft) == 1) edge = 1;
                
                float diffUp = 1.0 - dot(normalCenter, normalUp);
                if (diffUp >= _NormalThreshold && GetPriority(faceCenter, faceUp) == 1) edge = 1;
                
                float diffDown = 1.0 - dot(normalCenter, normalDown);
                if (diffDown >= _NormalThreshold && GetPriority(faceCenter, faceDown) == 1) edge = 1;
                
                return edge;
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
                
                // MODE 1: Imagen original (bypass del efecto)
                if (debugMode == 1)
                {
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                }
                
                // MODE 2: Depth map visualizado (usa _DepthNear y _DepthFar para contraste)
                if (debugMode == 2)
                {
                    float visualDepth = VisualizeDepth(uv);
                    return half4(visualDepth, visualDepth, visualDepth, 1);
                }
                
                // MODE 3: Normal map (RGB = XYZ normales)
                if (debugMode == 3)
                {
                    float3 normal = SampleSceneNormals(uv);
                    float3 visualNormal = normal * 0.5 + 0.5;
                    return half4(visualNormal, 1);
                }
                
                // MODE 4: Edges exteriores (depth) - ROJO sobre negro
                if (debugMode == 4)
                {
                    float edge = CalculateDepthEdge(uv, texelSize);
                    return half4(edge, 0, 0, 1);
                }
                
                // MODE 5: Edges interiores (normales) - VERDE sobre negro
                if (debugMode == 5)
                {
                    float edge = CalculateNormalEdge(uv, texelSize);
                    return half4(0, edge, 0, 1);
                }
                
                // MODE 6: Ambos edges (rojo = exteriores, verde = interiores)
                if (debugMode == 6)
                {
                    float depthEdge = CalculateDepthEdge(uv, texelSize);
                    float normalEdge = CalculateNormalEdge(uv, texelSize);
                    return half4(depthEdge, normalEdge, 0, 1);
                }
                
                // MODE 7: Imagen + edges exteriores (Darken)
                if (debugMode == 7)
                {
                    half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                    float edge = CalculateDepthEdge(uv, texelSize);
                    half3 finalColor = baseColor.rgb * (1.0 - edge * _DarkenAmount);
                    return half4(finalColor, baseColor.a);
                }
                
                // MODE 8: RESULTADO FINAL - exteriores (Darken) + interiores (Brighten)
                if (debugMode == 8)
                {
                    half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                    float depthEdge = CalculateDepthEdge(uv, texelSize);
                    float normalEdge = CalculateNormalEdge(uv, texelSize);
                    
                    half3 finalColor = baseColor.rgb * (1.0 - depthEdge * _DarkenAmount);
                    finalColor = finalColor + normalEdge * _BrightenAmount;
                    
                    return half4(saturate(finalColor), baseColor.a);
                }
                
                // Fallback
                return half4(1, 1, 0, 1);
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}