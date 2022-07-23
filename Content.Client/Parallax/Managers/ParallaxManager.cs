using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

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

    private CancellationTokenSource? _presentParallaxLoadCancel;

    private ParallaxLayerPrepared[] _parallaxLayersHQ = {};
    private ParallaxLayerPrepared[] _parallaxLayersLQ = {};

    public ParallaxLayerPrepared[] ParallaxLayers => _configurationManager.GetCVar(CCVars.ParallaxLowQuality) ? _parallaxLayersLQ : _parallaxLayersHQ;

    public async void LoadParallax()
    {
        await LoadParallaxByName("default");
    }

    private async Task LoadParallaxByName(string name)
    {
        // Update _parallaxName
        if (_parallaxName == name)
        {
            return;
        }
        _parallaxName = name;

        // Cancel any existing load and setup the new cancellation token
        _presentParallaxLoadCancel?.Cancel();
        _presentParallaxLoadCancel = new CancellationTokenSource();
        var cancel = _presentParallaxLoadCancel.Token;

        // Empty parallax name = no layers (this is so that the initial "" parallax name is consistent)
        if (_parallaxName == "")
        {
            _parallaxLayersHQ = _parallaxLayersLQ = new ParallaxLayerPrepared[] {};
            return;
        }

        // Begin (for real)
        Logger.InfoS("parallax", $"Loading parallax {name}");

        try
        {
            var parallaxPrototype = _prototypeManager.Index<ParallaxPrototype>(name);

            ParallaxLayerPrepared[] hq;
            ParallaxLayerPrepared[] lq;

            if (parallaxPrototype.LayersLQUseHQ)
            {
                lq = hq = await LoadParallaxLayers(parallaxPrototype.Layers, cancel);
            }
            else
            {
                var results = await Task.WhenAll(
                    LoadParallaxLayers(parallaxPrototype.Layers, cancel),
                    LoadParallaxLayers(parallaxPrototype.LayersLQ, cancel)
                );
                hq = results[0];
                lq = results[1];
            }

            // Still keeping this check just in case.
            if (_parallaxName == name)
            {
                _parallaxLayersHQ = hq;
                _parallaxLayersLQ = lq;
                Logger.InfoS("parallax", $"Loaded parallax {name}");
            }
        }
        catch (Exception ex)
        {
            Logger.ErrorS("parallax", $"Failed to loaded parallax {name}: {ex}");
        }
    }

    private async Task<ParallaxLayerPrepared[]> LoadParallaxLayers(List<ParallaxLayerConfig> layersIn, CancellationToken cancel = default)
    {
        // Because this is async, make sure it doesn't change (prototype reloads could muck this up)
        // Since the tasks aren't awaited until the end, this should be fine
        var tasks = new Task<ParallaxLayerPrepared>[layersIn.Count];
        for (var i = 0; i < layersIn.Count; i++)
        {
            tasks[i] = LoadParallaxLayer(layersIn[i], cancel);
        }
        return await Task.WhenAll(tasks);
    }

    private async Task<ParallaxLayerPrepared> LoadParallaxLayer(ParallaxLayerConfig config, CancellationToken cancel = default)
    {
        return new ParallaxLayerPrepared()
        {
            Texture = await config.Texture.GenerateTexture(cancel),
            Config = config
        };
    }
}

