using System.Threading;
using System.Threading.Tasks;
using Content.Client.IoC;
using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Data;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ImageParallaxTextureSource : IParallaxTextureSource
{
    /// <summary>
    /// Texture path.
    /// </summary>
    [DataField("path", required: true)]
    public ResPath Path { get; private set; } = default!;

    Task<Texture> IParallaxTextureSource.GenerateTexture(CancellationToken cancel)
    {
        return Task.FromResult(StaticIoC.ResC.GetTexture(Path));
    }
}

