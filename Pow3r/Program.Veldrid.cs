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

        private VeldridRenderer _vdRenderer = VeldridRenderer.Vulkan;

        private GraphicsDevice _vdGfxDevice;
        private CommandList _vdCommandList;
        private Pipeline _vdPipeline;
        private Shader[] _vdShaders;
        private ResourceSet _vdSetTexture;
        private ResourceSet _vdSetProjMatrix;
        private Texture _vdTexture;
        private Sampler _vdSampler;
        private DeviceBuffer _vdProjMatrixUniformBuffer;
        private int _vdLastWidth;
        private int _vdLastHeight;
        private VdFencedDatum[] _fencedData = Array.Empty<VdFencedDatum>();

        private void InitVeldrid()
        {
            var options = new GraphicsDeviceOptions
            {
#if DEBUG
                Debug = true,
#endif
                HasMainSwapchain = true,
                SyncToVerticalBlank = _vsync,
                PreferStandardClipSpaceYDirection = true,
                SwapchainSrgbFormat = true
            };

            GLFW.GetFramebufferSize(_window.WindowPtr, out var w, out var h);

            var hwnd = GLFW.GetWin32Window(_window.WindowPtr);
            var hinstance = GetModuleHandleA(null);

            switch (_vdRenderer)
            {
                case VeldridRenderer.Vulkan:
                    _vdGfxDevice = GraphicsDevice.CreateVulkan(
                        options,
                        VkSurfaceSource.CreateWin32((nint) hinstance, hwnd),
                        (uint) w, (uint) h);
                    break;
                case VeldridRenderer.D3D11:
                    _vdGfxDevice = GraphicsDevice.CreateD3D11(options, hwnd, (uint) w, (uint) h);
                    break;
                case VeldridRenderer.OpenGL:
                {
                    var platInfo = new OpenGLPlatformInfo(
                        (nint) _window.WindowPtr,
                        GLFW.GetProcAddress,
                        ptr => GLFW.MakeContextCurrent((Window*) ptr),
                        () => (nint) GLFW.GetCurrentContext(),
                        () => GLFW.MakeContextCurrent(null),
                        ptr => GLFW.DestroyWindow((Window*) ptr),
                        () => GLFW.SwapBuffers(_window.WindowPtr),
                        vsync => GLFW.SwapInterval(vsync ? 1 : 0));

                    _vdGfxDevice = GraphicsDevice.CreateOpenGL(options, platInfo, (uint) w, (uint) h);
                    break;
                }
            }


            var factory = _vdGfxDevice.ResourceFactory;

            _vdCommandList = factory.CreateCommandList();
            _vdCommandList.Name = "Honk";

            var vtxLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementFormat.Float2,
                    VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("UV", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("Color", VertexElementFormat.Byte4_Norm,
                    VertexElementSemantic.TextureCoordinate));

            var vtxShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VDVertexShader),
                "main");

            var fragShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(VDFragmentShader),
                "main");

            _vdShaders = factory.CreateFromSpirv(vtxShaderDesc, fragShaderDesc);

            _vdShaders[0].Name = "VertexShader";
            _vdShaders[1].Name = "FragmentShader";

            var layoutTexture = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment),
                new ResourceLayoutElementDescription(
                    "TextureSampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment)));

            layoutTexture.Name = "LayoutTexture";

            var layoutProjMatrix = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ProjMtx",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex)));

            layoutProjMatrix.Name = "LayoutProjMatrix";

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
            _vdPipeline.Name = "MainPipeline";

            _vdProjMatrixUniformBuffer = factory.CreateBuffer(new BufferDescription(
                (uint) sizeof(Matrix4x4),
                BufferUsage.Dynamic | BufferUsage.UniformBuffer));
            _vdProjMatrixUniformBuffer.Name = "_vdProjMatrixUniformBuffer";

            _vdSetProjMatrix = factory.CreateResourceSet(new ResourceSetDescription(
                layoutProjMatrix,
                _vdProjMatrixUniformBuffer));
            _vdSetProjMatrix.Name = "_vdSetProjMatrix";
            var io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out _);

            _vdTexture = factory.CreateTexture(TextureDescription.Texture2D(
                (uint) width, (uint) height,
                mipLevels: 1,
                arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
                TextureUsage.Sampled));

            _vdTexture.Name = "MainTexture";

            _vdSampler = factory.CreateSampler(SamplerDescription.Linear);

            _vdSampler.Name = "MainSampler";

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

            _vdSetTexture.Name = "SetTexture";

            io.Fonts.SetTexID(0);
            io.Fonts.ClearTexData();

            _vdGfxDevice.ResizeMainWindow((uint) w, (uint) h);
            _vdGfxDevice.SwapBuffers();
        }

        private void RenderVeldrid()
        {
            GLFW.GetFramebufferSize(_window.WindowPtr, out var fbW, out var fbH);

            if (_vdLastWidth != fbW && _vdLastHeight != fbH)
            {
                _vdGfxDevice.ResizeMainWindow((uint) fbW, (uint) fbH);
                _vdLastWidth = fbW;
                _vdLastHeight = fbH;
            }

            _vdCommandList.Begin();
            _vdCommandList.SetFramebuffer(_vdGfxDevice.SwapchainFramebuffer);

            _vdCommandList.SetViewport(0, new Viewport(0, 0, fbW, fbH, 0, 1));
            _vdCommandList.ClearColorTarget(0, RgbaFloat.Black);

            var factory = _vdGfxDevice.ResourceFactory;

            var drawData = ImGui.GetDrawData();

            ref var fencedData = ref GetFreeFencedData();
            ref var vtxBuf = ref fencedData.VertexBuffer;
            ref var idxBuf = ref fencedData.IndexBuffer;

            var byteLenVtx = (uint) (sizeof(ImDrawVert) * drawData.TotalVtxCount);
            if (fencedData.VertexBuffer == null || vtxBuf.SizeInBytes < byteLenVtx)
            {
                vtxBuf?.Dispose();
                vtxBuf = factory.CreateBuffer(new BufferDescription(
                    byteLenVtx,
                    BufferUsage.VertexBuffer | BufferUsage.Dynamic));
                vtxBuf.Name = "_vdVtxBuffer";
            }

            var byteLenIdx = (uint) (sizeof(ushort) * drawData.TotalIdxCount);
            if (idxBuf == null || idxBuf.SizeInBytes < byteLenIdx)
            {
                idxBuf?.Dispose();
                idxBuf = factory.CreateBuffer(new BufferDescription(
                    byteLenIdx,
                    BufferUsage.IndexBuffer | BufferUsage.Dynamic));
                idxBuf.Name = "_vdIdxBuffer";
            }

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

            _vdGfxDevice.Unmap(vtxBuf);
            _vdGfxDevice.Unmap(idxBuf);

            _vdCommandList.End();

            _vdGfxDevice.SubmitCommands(_vdCommandList, fencedData.Fence);
            _vdGfxDevice.SwapBuffers();
        }

        private ref VdFencedDatum GetFreeFencedData()
        {
            for (var i = 0; i < _fencedData.Length; i++)
            {
                ref var fenced = ref _fencedData[i];

                if (fenced.Fence.Signaled)
                {
                    fenced.Fence.Reset();
                    return ref fenced;
                }
            }

            Array.Resize(ref _fencedData, _fencedData.Length + 1);
            ref var slot = ref _fencedData[^1];
            slot = new VdFencedDatum {Fence = _vdGfxDevice.ResourceFactory.CreateFence(false)};
            return ref slot;
        }

        private static Span<T> MappedToSpan<T>(MappedResourceView<T> mapped) where T : struct
        {
            return MemoryMarshal.CreateSpan(ref mapped[0], mapped.Count);
        }

        [DllImport("kernel32.dll")]
        private static extern void* GetModuleHandleA(byte* lpModuleName);

        private struct VdFencedDatum
        {
            public Fence Fence;

            public DeviceBuffer IndexBuffer;
            public DeviceBuffer VertexBuffer;
        }

        private enum VeldridRenderer
        {
            Vulkan,
            D3D11,
            OpenGL
        }
    }
}
