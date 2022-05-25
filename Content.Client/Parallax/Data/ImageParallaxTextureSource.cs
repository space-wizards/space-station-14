using System.Threading;
using System.Threading.Tasks;
﻿using System.Collections.Generic;
using JetBrains.Annotations;
using Content.Client.Resources;
using Content.Client.IoC;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Data;

[UsedImplicitly]
[DataDefinition]
public sealed class ImageParallaxTextureSource : IParallaxTextureSource
{
    /// <summary>
    /// Texture path.
    /// </summary>
    [DataField("path", required: true)]
    public ResourcePath Path { get; } = default!;

    async Task<Texture> IParallaxTextureSource.GenerateTexture(CancellationToken cancel = default)
    {
        return StaticIoC.ResC.GetTexture(Path);
    }
}

