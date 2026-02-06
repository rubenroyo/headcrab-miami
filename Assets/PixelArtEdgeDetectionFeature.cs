using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class PixelArtEdgeDetectionFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material edgeDetectionMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Range(0f, 5f)] public float depthThreshold = 1.5f;
        [Range(0f, 5f)] public float normalThreshold = 0.4f;
        [Range(0.5f, 3f)] public float edgeThickness = 1f;
        public Color depthEdgeColor = Color.black;
        public Color normalEdgeColor = Color.white;
    }

    public Settings settings = new Settings();
    PixelArtEdgeDetectionPass pass;

    public override void Create()
    {
        pass = new PixelArtEdgeDetectionPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.edgeDetectionMaterial == null)
        {
            Debug.LogWarning("PixelArtEdgeDetectionFeature: No material assigned!");
            return;
        }
        
        // Solo aplicar en cámaras que renderizen geometría (no en preview, reflection, etc.)
        if (renderingData.cameraData.cameraType != CameraType.Game && 
            renderingData.cameraData.cameraType != CameraType.SceneView)
            return;
        
        // Requerir depth y normal textures
        pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        renderer.EnqueuePass(pass);
    }

    class PixelArtEdgeDetectionPass : ScriptableRenderPass
    {
        private Settings settings;
        private Material material;

        // Data class for Render Graph
        private class PassData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public Material material;
        }

        public PixelArtEdgeDetectionPass(Settings settings)
        {
            this.settings = settings;
            this.material = settings.edgeDetectionMaterial;
            renderPassEvent = settings.renderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // Skip if no valid source
            if (resourceData.activeColorTexture.IsValid() == false)
                return;

            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            // Create temp texture
            TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, descriptor, "_EdgeDetectionTemp", false);

            // Set material properties
            material.SetFloat("_DepthThreshold", settings.depthThreshold);
            material.SetFloat("_NormalThreshold", settings.normalThreshold);
            material.SetFloat("_EdgeThickness", settings.edgeThickness);
            material.SetColor("_DepthEdgeColor", settings.depthEdgeColor);
            material.SetColor("_NormalEdgeColor", settings.normalEdgeColor);

            // First blit: source -> temp with edge detection
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection Pass", out var passData))
            {
                passData.source = resourceData.activeColorTexture;
                passData.destination = tempTexture;
                passData.material = material;

                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Second blit: temp -> source (copy back)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection Copy Back", out var passData))
            {
                passData.source = tempTexture;
                passData.destination = resourceData.activeColorTexture;
                passData.material = material;

                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }
}