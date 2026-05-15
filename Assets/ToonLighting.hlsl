#ifndef TOON_LIGHTING_INCLUDED
#define TOON_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void ToonLighting_float(
    float3 WorldPos,
    float3 WorldNormal,
    float Steps,
    out float3 ToonDiffuse
)
{
    ToonDiffuse = float3(0, 0, 0);

    // Luz principal
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light mainLight    = GetMainLight(shadowCoord);
    float NdotL        = saturate(dot(WorldNormal, mainLight.direction));
    float toon         = floor(NdotL * Steps) / Steps;
    ToonDiffuse        = mainLight.color * toon;

    // GI / Baked
#if defined(LIGHTMAP_ON)
    float3 bakedGI = SampleLightmap(LightmapUV, float2(0,0), WorldNormal);
#else
    float3 bakedGI = SampleSHPixel(WorldNormal, WorldNormal);
#endif
    float bakedLuma = dot(bakedGI, float3(0.299, 0.587, 0.114));
    float toonBaked = floor(bakedLuma * Steps) / Steps;
    bakedGI         = bakedGI * (toonBaked / max(bakedLuma, 0.0001));
    ToonDiffuse    += bakedGI;

    // Point lights y spot lights
#if defined(_ADDITIONAL_LIGHTS)
    uint lightsCount = GetAdditionalLightsCount();
    for (uint i = 0u; i < lightsCount; i++)
    {
        Light light     = GetAdditionalLight(i, WorldPos);
        float NdotL_add = saturate(dot(WorldNormal, light.direction));
        float atten     = light.distanceAttenuation * light.shadowAttenuation;
        float toon_add  = floor(NdotL_add * atten * Steps) / Steps;
        ToonDiffuse    += light.color * toon_add;
    }
#endif
}

void ToonLighting_half(
    half3 WorldPos,
    half3 WorldNormal,
    half Steps,
    out half3 ToonDiffuse
)
{
    float3 d;
    ToonLighting_float(WorldPos, WorldNormal, Steps, d);
    ToonDiffuse = d;
}

#endif