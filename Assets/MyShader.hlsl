void GetMainLightData_float(float3 WorldPos, out float3 Direction, out float3 Color, out float ShadowAtten)
{
    #ifdef SHADERGRAPH_PREVIEW
        Direction = float3(0.5, 0.5, 0);
        Color = float3(1, 1, 1);
        ShadowAtten = 1;
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        Light mainLight = GetMainLight(shadowCoord);
        Direction = mainLight.direction;
        Color = mainLight.color;
        ShadowAtten = mainLight.shadowAttenuation;
    #endif
}