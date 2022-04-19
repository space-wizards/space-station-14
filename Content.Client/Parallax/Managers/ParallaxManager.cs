using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.Client.Parallax.Data;
using Content.Shared;
using Content.Shared.CCVar;
using Nett;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private string _parallaxName = "";
    public string ParallaxName
    {
        get => _parallaxName;
        set
        {
            LoadParallaxByName(value);
        }
    }

    public Vector2 ParallaxAnchor { get; set; }

    public ParallaxLayerPrepared[] ParallaxLayers { get; private set; } = {};

    public async void LoadParallax()
    {
        await LoadParallaxByName("default");
    }

    private async Task LoadParallaxByName(string name)
    {
        _parallaxName = name;
        Logger.InfoS("parallax", $"Loading parallax {name}");

        var parallaxPrototype = _prototypeManager.Index<ParallaxPrototype>(name);
        // just in case the length were to change during loading
        var layersIn = parallaxPrototype.Layers.ToArray();
        var layers = new ParallaxLayerPrepared[layersIn.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            layers[i] = await LoadParallaxLayer(layersIn[i]);
        }

        if (_parallaxName == name)
        {
            ParallaxLayers = layers;
            Logger.InfoS("parallax", $"Loaded parallax {name}");
        }
        else
        {
            Logger.InfoS("parallax", $"Loaded parallax {name}, but the target changed while it was being loaded.");
        }
    }

    private async Task<ParallaxLayerPrepared> LoadParallaxLayer(ParallaxLayerConfig config)
    {
        return new ParallaxLayerPrepared()
        {
            Texture = await config.Texture.GenerateTexture(),
            Config = config
        };
    }
}

