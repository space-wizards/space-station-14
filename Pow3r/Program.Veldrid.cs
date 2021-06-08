using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Veldrid;
using Veldrid.OpenGL;
using Veldrid.SPIRV;
using Veldrid.Vk;

namespace Pow3r
{
    internal sealed unsafe partial class Program
    {
        private const string VDVertexShader = @"
#version 460

layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec4 Color;

layout (set = 0, binding = 0) uniform ProjMtx {
    mat4 _ProjMtx;
};

layout (location = 0) out vec2 Frag_UV;
layout (location = 1) out vec4 Frag_Color;

// Converts a color from sRGB gamma to linear light gamma
vec4 toLinear(vec4 sRGB)
{
    bvec3 cutoff = lessThan(sRGB.rgb, vec3(0.04045));
    vec3 higher = pow((sRGB.rgb + vec3(0.055))/vec3(1.055), vec3(2.4));
    vec3 lower = sRGB.rgb/vec3(12.92);

    return vec4(mix(higher, lower, cutoff), sRGB.a);
}

void main()
{
    Frag_UV = UV;
    Frag_Color = toLinear(Color);
    gl_Position = _ProjMtx * vec4(Position.xy,0,1);
}";

        private const string VDFragmentShader = @"
#version 460

layout (location = 0) in vec2 Frag_UV;
layout (location = 1) in vec4 Frag_Color;

layout (set = 1, binding = 0) uniform texture2D Texture;
layout (set = 1, binding = 1) uniform sampler TextureSampler;

layout (location = 0) out vec4 Out_Color;

void main()
{
    Out_Color = Frag_Color * texture(sampler2D(Texture, TextureSampler), Frag_UV.st);
}";

        private GraphicsDevice _vdGfxDevice;
        private CommandList _vdCommandList;
        private Pipeline _vdPipeline;
        private Shader[] _vdShaders;
        private ResourceSet _vdSetTexture;
        private ResourceSet _vdSetProjMatrix;
        private Texture _vdTexture;
        private Sampler _vdSampler;
        private DeviceBuffer _vdProjMatrixUniformBuffer;

        private void InitVeldrid()
        {
            var options = new GraphicsDeviceOptions
            {
#if DEBUG
                Debug = true,
#endif
                HasMainSwapchain = true,
                SyncToVerticalBlank = true,
                PreferStandardClipSpaceYDirection = true,
                SwapchainSrgbFormat = true
            };

            GLFW.GetFramebufferSize(_window.WindowPtr, out var w, out var h);

            var hwnd = GLFW.GetWin32Window(_window.WindowPtr);
            var hinstance = GetModuleHandleA(null);

            _vdGfxDevice = GraphicsDevice.CreateVulkan(
                options,
                VkSurfaceSource.CreateWin32((nint) hinstance, hwnd),
                (uint) w, (uint) h);

            // _vdGfxDevice = GraphicsDevice.CreateD3D11(options, hwnd, (uint) w, (uint) h);

            var factory = _vdGfxDevice.ResourceFactory;

            _vdCommandList = factory.CreateCommandList();

            var vtxLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.Position),
                new VertexElementDescription("UV", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("Color", VertexElementFormat.Byte4_Norm, VertexElementSemantic.Color));

            var vtxShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VDVertexShader),
                "main");

            var fragShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(VDFragmentShader),
                "main");

            _vdShaders = factory.CreateFromSpirv(vtxShaderDesc, fragShaderDesc);

            var layoutTexture = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment),
                new ResourceLayoutElementDescription(
                    "TextureSampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment)));

            var layoutProjMatrix = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ProjMtx",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex)));

            var pipelineDesc = new GraphicsPipelineDescription(
                new BlendStateDescription(
                    RgbaFloat.White,
                    new BlendAttachmentDescription(
                        true,
                        BlendFactor.SourceAlpha,
                        BlendFactor.InverseSourceAlpha,
                        BlendFunction.Add,
                        BlendFactor.One,
                        BlendFactor.InverseSourceAlpha,
                        BlendFunction.Add)
                ),
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(
                    FaceCullMode.None,
                    PolygonFillMode.Solid,
                    FrontFace.Clockwise,
                    depthClipEnabled: false,
                    scissorTestEnabled: true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(new[] {vtxLayout}, _vdShaders),
                new[] {layoutProjMatrix, layoutTexture},
                new OutputDescription(
                    null,
                    new OutputAttachmentDescription(PixelFormat.B8_G8_R8_A8_UNorm_SRgb))
            );

            _vdPipeline = factory.CreateGraphicsPipeline(pipelineDesc);

            _vdProjMatrixUniformBuffer = factory.CreateBuffer(new BufferDescription(
                (uint) sizeof(Matrix4x4),
                BufferUsage.Dynamic | BufferUsage.UniformBuffer));

            _vdSetProjMatrix = factory.CreateResourceSet(new ResourceSetDescription(
                layoutProjMatrix,
                _vdProjMatrixUniformBuffer));

            var io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out _);

            _vdTexture = factory.CreateTexture(TextureDescription.Texture2D(
                (uint) width, (uint) height,
                mipLevels: 1,
                arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
                TextureUsage.Sampled));

            _vdSampler = factory.CreateSampler(SamplerDescription.Linear);

            _vdGfxDevice.UpdateTexture(
                _vdTexture,
                (IntPtr) pixels,
                (uint) (width * height * 4),
                x: 0, y: 0, z: 0,
                (uint) width, (uint) height, depth: 1,
                mipLevel: 0,
                arrayLayer: 0);

            _vdSetTexture = factory.CreateResourceSet(new ResourceSetDescription(
                layoutTexture,
                _vdTexture,
                _vdSampler));

            io.Fonts.SetTexID((nint) 0);
            io.Fonts.ClearTexData();
        }

        private void RenderVeldrid()
        {
            _vdCommandList.Begin();
            _vdCommandList.SetFramebuffer(_vdGfxDevice.SwapchainFramebuffer);

            GLFW.GetFramebufferSize(_window.WindowPtr, out var fbW, out var fbH);

            _vdCommandList.SetViewport(0, new Viewport(0, 0, fbW, fbH, 0, 1));
            _vdCommandList.ClearColorTarget(0, RgbaFloat.Black);

            var factory = _vdGfxDevice.ResourceFactory;

            var drawData = ImGui.GetDrawData();

            var vtxBuf = factory.CreateBuffer(new BufferDescription(
                (uint) (sizeof(ImDrawVert) * drawData.TotalVtxCount),
                BufferUsage.VertexBuffer | BufferUsage.Dynamic));

            var idxBuf = factory.CreateBuffer(new BufferDescription(
                (uint) (sizeof(ushort) * drawData.TotalIdxCount),
                BufferUsage.IndexBuffer | BufferUsage.Dynamic));

            var vtxOffset = 0;
            var idxOffset = 0;
            var mappedVtxBuf = MappedToSpan(_vdGfxDevice.Map<ImDrawVert>(vtxBuf, MapMode.Write));
            var mappedIdxBuf = MappedToSpan(_vdGfxDevice.Map<ushort>(idxBuf, MapMode.Write));

            var l = drawData.DisplayPos.X;
            var r = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var t = drawData.DisplayPos.Y;
            var b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            var matrix = Matrix4x4.CreateOrthographicOffCenter(l, r, b, t, -1, 1);

            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            _vdCommandList.UpdateBuffer(_vdProjMatrixUniformBuffer, 0, ref matrix);

            _vdCommandList.SetPipeline(_vdPipeline);
            _vdCommandList.SetGraphicsResourceSet(0, _vdSetProjMatrix);
            _vdCommandList.SetGraphicsResourceSet(1, _vdSetTexture);
            _vdCommandList.SetVertexBuffer(0, vtxBuf);
            _vdCommandList.SetIndexBuffer(idxBuf, IndexFormat.UInt16);

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var drawList = drawData.CmdListsRange[n];

                var drawVtx = new Span<ImDrawVert>((void*) drawList.VtxBuffer.Data, drawList.VtxBuffer.Size);
                var drawIdx = new Span<ushort>((void*) drawList.IdxBuffer.Data, drawList.IdxBuffer.Size);

                drawVtx.CopyTo(mappedVtxBuf[vtxOffset..]);
                drawIdx.CopyTo(mappedIdxBuf[idxOffset..]);

                for (var cmdI = 0; cmdI < drawList.CmdBuffer.Size; cmdI++)
                {
                    var cmd = drawList.CmdBuffer[cmdI];

                    Vector4 clipRect = default;
                    clipRect.X = (cmd.ClipRect.X - clipOff.X) * clipScale.X;
                    clipRect.Y = (cmd.ClipRect.Y - clipOff.Y) * clipScale.Y;
                    clipRect.Z = (cmd.ClipRect.Z - clipOff.X) * clipScale.X;
                    clipRect.W = (cmd.ClipRect.W - clipOff.Y) * clipScale.Y;

                    _vdCommandList.SetScissorRect(
                        0,
                        (uint) clipRect.X,
                        (uint) clipRect.Y,
                        (uint) (clipRect.Z - clipRect.X),
                        (uint) (clipRect.W - clipRect.Y));

                    _vdCommandList.DrawIndexed(
                        cmd.ElemCount,
                        1,
                        (uint) (cmd.IdxOffset + idxOffset),
                        (int) (cmd.VtxOffset + vtxOffset),
                        0);
                }

                vtxOffset += drawVtx.Length;
                idxOffset += drawIdx.Length;
            }

            _vdCommandList.End();

            _vdGfxDevice.Unmap(vtxBuf);
            _vdGfxDevice.Unmap(idxBuf);

            _vdGfxDevice.SubmitCommands(_vdCommandList);
            _vdGfxDevice.SwapBuffers();

            vtxBuf.Dispose();
            idxBuf.Dispose();
        }

        private static Span<T> MappedToSpan<T>(MappedResourceView<T> mapped) where T : struct
        {
            return MemoryMarshal.CreateSpan(ref mapped[0], mapped.Count);
        }

        [DllImport("kernel32.dll")]
        private static extern void* GetModuleHandleA(byte* lpModuleName);
    }
}
