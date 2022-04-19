using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.Shared;
using Content.Shared.CCVar;
using Nett;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Parallax.Managers;

internal sealed class ParallaxManager : IParallaxManager
{
    public ParallaxLayerPrepared[] ParallaxLayers { get; private set; } = {};

    public async void LoadParallax()
    {
        // nyi
    }
}

