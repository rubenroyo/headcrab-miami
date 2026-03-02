Shader "Custom/PixelArtEdgeDetectionDebug"
{
    Properties
    {
        _DebugMode ("Debug Mode", Range(0, 8)) = 0
        _DepthThreshold ("Depth Threshold", Range(0, 5)) = 1.5
        _DepthAngleMultiplier ("Depth Angle Multiplier", Range(1, 10)) = 4
        _NormalThreshold ("Normal Threshold", Range(0, 2)) = 0.5
        _EdgeThickness ("Edge Thickness", Range(0.5, 3)) = 1
        _DepthNear ("Depth Near", Range(0, 50)) = 8
        _DepthFar ("Depth Far", Range(1, 100)) = 15
        _DarkenAmount ("Darken Amount", Range(0, 1)) = 0.85
        _BrightenAmount ("Brighten Amount", Range(0, 1)) = 0.3
        _HueInner ("Hue Inner Target", Range(0, 1)) = 0.166  // Amarillo (~60°/360°)
        _HueOuter ("Hue Outer Target", Range(0, 1)) = 0.666  // Azul (~240°/360°)
        _HueShiftAmount ("Hue Shift Amount", Range(0, 0.5)) = 0.1
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
            float _DepthAngleMultiplier;
            float _EdgeThickness;
            float _DepthNear;
            float _DepthFar;
            float _DarkenAmount;
            float _BrightenAmount;
            float _HueInner;
            float _HueOuter;
            float _HueShiftAmount;
            
            // ========== RGB <-> HSV Conversion ==========
            float3 RGBtoHSV(float3 rgb)
            {
                float maxC = max(rgb.r, max(rgb.g, rgb.b));
                float minC = min(rgb.r, min(rgb.g, rgb.b));
                float delta = maxC - minC;
                
                float h = 0;
                float s = (maxC > 0) ? (delta / maxC) : 0;
                float v = maxC;
                
                if (delta > 0.00001)
                {
                    if (maxC == rgb.r)
                        h = (rgb.g - rgb.b) / delta + (rgb.g < rgb.b ? 6.0 : 0.0);
                    else if (maxC == rgb.g)
                        h = (rgb.b - rgb.r) / delta + 2.0;
                    else
                        h = (rgb.r - rgb.g) / delta + 4.0;
                    h /= 6.0;
                }
                
                return float3(h, s, v);
            }
            
            float3 HSVtoRGB(float3 hsv)
            {
                float h = hsv.x * 6.0;
                float s = hsv.y;
                float v = hsv.z;
                
                float c = v * s;
                float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
                float m = v - c;
                
                float3 rgb;
                if (h < 1.0)      rgb = float3(c, x, 0);
                else if (h < 2.0) rgb = float3(x, c, 0);
                else if (h < 3.0) rgb = float3(0, c, x);
                else if (h < 4.0) rgb = float3(0, x, c);
                else if (h < 5.0) rgb = float3(x, 0, c);
                else              rgb = float3(c, 0, x);
                
                return rgb + m;
            }
            
            // Calcula distancia con signo en la rueda de hue (circular)
            // Retorna valor entre -0.5 y 0.5, positivo = sentido horario
            float HueDistance(float from, float to)
            {
                float diff = to - from;
                // Normalizar a rango [-0.5, 0.5]
                if (diff > 0.5) diff -= 1.0;
                if (diff < -0.5) diff += 1.0;
                return diff;
            }
            
            // Aplica el shift de hue según la lógica de caminos
            // innerEdge: 1 si es borde interior, outerEdge: 1 si es borde exterior
            float3 ApplyHueShift(float3 rgb, float innerEdge, float outerEdge)
            {
                float3 hsv = RGBtoHSV(rgb);
                float currentHue = hsv.x;
                
                // Calcular distancias con signo hacia cada objetivo
                float distToInner = HueDistance(currentHue, _HueInner);
                float distToOuter = HueDistance(currentHue, _HueOuter);
                
                // Direcciones de camino corto (signo)
                float dirInner = sign(distToInner);  // +1 = horario, -1 = antihorario
                float dirOuter = sign(distToOuter);
                
                float hueShift = 0;
                float valueShift = 0;
                
                if (innerEdge > 0.5)
                {
                    // Borde interior: subir brillo, mover hacia HueInner
                    valueShift = _BrightenAmount;
                    
                    // Si ambos van en la misma dirección, interior usa camino corto
                    // Si van en direcciones distintas, cada uno usa su camino corto
                    if (dirInner == dirOuter && dirInner != 0)
                    {
                        // Misma dirección: interior va por camino corto (normal)
                        hueShift = dirInner * _HueShiftAmount;
                    }
                    else
                    {
                        // Direcciones distintas: usar camino corto hacia inner
                        hueShift = dirInner * _HueShiftAmount;
                    }
                }
                
                if (outerEdge > 0.5)
                {
                    // Borde exterior: bajar brillo, mover hacia HueOuter
                    valueShift = -_DarkenAmount;
                    
                    if (dirInner == dirOuter && dirOuter != 0)
                    {
                        // Misma dirección: exterior va por camino LARGO (opuesto)
                        hueShift = -dirOuter * _HueShiftAmount;
                    }
                    else
                    {
                        // Direcciones distintas: usar camino corto hacia outer
                        hueShift = dirOuter * _HueShiftAmount;
                    }
                }
                
                // Aplicar cambios
                hsv.x = frac(hsv.x + hueShift); // frac para mantener en [0,1]
                hsv.z = saturate(hsv.z + valueShift);
                
                return HSVtoRGB(hsv);
            }
            
            // Robert's Cross edge detection para depth
            // Usa muestras diagonales: TL, TR, BL, BR
            // Solo pinta como edge el pixel MÁS CERCANO
            // Threshold variable según ángulo de superficie
            float CalculateDepthEdge(float2 uv, float2 texelSize)
            {
                // Muestras diagonales
                float depthTL = LinearEyeDepth(SampleSceneDepth(uv + float2(-texelSize.x, texelSize.y)), _ZBufferParams);
                float depthTR = LinearEyeDepth(SampleSceneDepth(uv + float2(texelSize.x, texelSize.y)), _ZBufferParams);
                float depthBL = LinearEyeDepth(SampleSceneDepth(uv + float2(-texelSize.x, -texelSize.y)), _ZBufferParams);
                float depthBR = LinearEyeDepth(SampleSceneDepth(uv + float2(texelSize.x, -texelSize.y)), _ZBufferParams);
                float depthCenter = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                
                // Robert's Cross: diferencias cruzadas
                float diff1 = depthTL - depthBR;
                float diff2 = depthTR - depthBL;
                float robertsCross = sqrt(diff1 * diff1 + diff2 * diff2);
                
                // Leer normal para ajustar threshold según ángulo
                float3 normal = SampleSceneNormals(uv);
                // En view space, normal.z indica cuánto apunta hacia la cámara
                // z grande = de frente (threshold normal), z pequeño = inclinado (threshold alto)
                float viewAngle = 1.0 - saturate(normal.z); // 0 = de frente, 1 = muy inclinado
                float adjustedThreshold = _DepthThreshold * (1.0 + viewAngle * (_DepthAngleMultiplier - 1.0));
                
                // Solo pintar si el centro es el más cercano de todos los vecinos
                float minNeighbor = min(min(depthTL, depthTR), min(depthBL, depthBR));
                float isCenterCloser = step(depthCenter, minNeighbor);
                
                // Threshold ajustado y decisión final
                float edge = step(adjustedThreshold, robertsCross) * isCenterCloser;
                
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
            
            // Robert's Cross para normales con tabla de prioridad
            float CalculateNormalEdge(float2 uv, float2 texelSize)
            {
                // Muestras diagonales
                float3 normalTL = SampleSceneNormals(uv + float2(-texelSize.x, texelSize.y));
                float3 normalTR = SampleSceneNormals(uv + float2(texelSize.x, texelSize.y));
                float3 normalBL = SampleSceneNormals(uv + float2(-texelSize.x, -texelSize.y));
                float3 normalBR = SampleSceneNormals(uv + float2(texelSize.x, -texelSize.y));
                float3 normalCenter = SampleSceneNormals(uv);
                
                int faceCenter = ClassifyNormal(normalCenter);
                int faceTL = ClassifyNormal(normalTL);
                int faceTR = ClassifyNormal(normalTR);
                int faceBL = ClassifyNormal(normalBL);
                int faceBR = ClassifyNormal(normalBR);
                
                // Robert's Cross: diferencias cruzadas usando dot product
                float3 diff1 = normalTL - normalBR;
                float3 diff2 = normalTR - normalBL;
                float robertsCross = sqrt(dot(diff1, diff1) + dot(diff2, diff2));
                
                // Solo pintar si supera threshold Y el centro "pierde" contra algún vecino diagonal
                float edge = 0;
                if (robertsCross >= _NormalThreshold)
                {
                    if (GetPriority(faceCenter, faceTL) == 1) edge = 1;
                    if (GetPriority(faceCenter, faceTR) == 1) edge = 1;
                    if (GetPriority(faceCenter, faceBL) == 1) edge = 1;
                    if (GetPriority(faceCenter, faceBR) == 1) edge = 1;
                }
                
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
                
                // MODE 8: RESULTADO FINAL - Hue shift + brillo
                if (debugMode == 8)
                {
                    half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                    float depthEdge = CalculateDepthEdge(uv, texelSize);   // Borde exterior
                    float normalEdge = CalculateNormalEdge(uv, texelSize); // Borde interior
                    
                    half3 finalColor = ApplyHueShift(baseColor.rgb, normalEdge, depthEdge);
                    
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