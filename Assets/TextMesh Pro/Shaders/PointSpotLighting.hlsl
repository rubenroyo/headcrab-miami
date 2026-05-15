#ifndef POINTSPOT_LIGHTING_INCLUDED
#define POINTSPOT_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void PointSpotLighting_float(
    float3 WorldPos,
    float3 WorldNormal,
    out float3 Color)
{
    Color = float3(0, 0, 0);

    uint lightCount = GetAdditionalLightsCount();

    LIGHT_LOOP_BEGIN(lightCount)
        Light light = GetAdditionalLight(lightIndex, WorldPos, half4(1,1,1,1));
        float NdotL = saturate(dot(normalize(WorldNormal), light.direction));
        float atten = light.distanceAttenuation * light.shadowAttenuation;
        Color += light.color * NdotL * atten;
    LIGHT_LOOP_END
}

void DirectionalLighting_float(
    float3 WorldPos,
    float3 WorldNormal,
    out float3 Color)
{
    Color = float3(0, 0, 0);

#ifdef SHADERGRAPH_PREVIEW
    Color = float3(0.5, 0.5, 0.5);
    return;
#endif

    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light light = GetMainLight(shadowCoord);
    float NdotL = saturate(dot(normalize(WorldNormal), light.direction));
    Color = light.color * NdotL * light.shadowAttenuation;
}

void AllLighting_float(
    float3 WorldPos,
    float3 WorldNormal,
    out float3 Color)
{
    Color = float3(0, 0, 0);

#ifdef SHADERGRAPH_PREVIEW
    Color = float3(0.5, 0.5, 0.5);
    return;
#endif

    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light mainLight = GetMainLight(shadowCoord);
    float NdotL = saturate(dot(normalize(WorldNormal), mainLight.direction));
    Color += mainLight.color * NdotL * mainLight.shadowAttenuation;

    uint lightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(lightCount)
        Light light = GetAdditionalLight(lightIndex, WorldPos, half4(1,1,1,1));
        float NdotL2 = saturate(dot(normalize(WorldNormal), light.direction));
        float atten = light.distanceAttenuation * light.shadowAttenuation;
        Color += light.color * NdotL2 * atten;
    LIGHT_LOOP_END
}

#endif