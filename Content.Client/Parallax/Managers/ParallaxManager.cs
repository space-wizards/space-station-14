using System.Collections.Concurrent;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Client.Parallax.Data;
using Content.Shared.CCVar;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Managers;

public sealed class ParallaxManager : IParallaxManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private ISawmill _sawmill = Logger.GetSawmill("parallax");

    public Vector2 ParallaxAnchor { get; set; }

    private readonly Dictionary<string, ParallaxLayerPrepared[]> _parallaxesLQ = new();
    private readonly Dictionary<string, ParallaxLayerPrepared[]> _parallaxesHQ = new();

    private readonly Dictionary<string, CancellationTokenSource> _loadingParallaxes = new();

    public bool IsLoaded(string name) => _parallaxesLQ.ContainsKey(name);

    public ParallaxLayerPrepared[] GetParallaxLayers(string name)
    {
        if (_configurationManager.GetCVar(CCVars.ParallaxLowQuality))
        {
            return !_parallaxesLQ.TryGetValue(name, out var lq) ? Array.Empty<ParallaxLayerPrepared>() : lq;
        }

        return !_parallaxesHQ.TryGetValue(name, out var hq) ? Array.Empty<ParallaxLayerPrepared>() : hq;
    }

    public void UnloadParallax(string name)
    {
        if (_loadingParallaxes.TryGetValue(name, out var loading))
        {
            loading.Cancel();
            _loadingParallaxes.Remove(name, out _);
            return;
        }

        if (!_parallaxesLQ.ContainsKey(name)) return;
        _parallaxesLQ.Remove(name);
        _parallaxesHQ.Remove(name);
    }

    public async void LoadDefaultParallax()
    {
        _sawmill.Level = LogLevel.Info;
        await LoadParallaxByName("Default");
    }

    public async Task LoadParallaxByName(string name)
    {
        if (_parallaxesLQ.ContainsKey(name) || _loadingParallaxes.ContainsKey(name)) return;

        // Cancel any existing load and setup the new cancellation token
        var token = new CancellationTokenSource();
        _loadingParallaxes[name] = token;
        var cancel = token.Token;

        // Begin (for real)
        _sawmill.Debug($"Loading parallax {name}");

        try
        {
            var parallaxPrototype = _prototypeManager.Index<ParallaxPrototype>(name);

            ParallaxLayerPrepared[][] layers;

            if (parallaxPrototype.LayersLQUseHQ)
            {
                layers = new ParallaxLayerPrepared[2][];
                layers[0] = layers[1] = await LoadParallaxLayers(parallaxPrototype.Layers, cancel);
            }
            else
            {
                layers = await Task.WhenAll(
                    LoadParallaxLayers(parallaxPrototype.Layers, cancel),
                    LoadParallaxLayers(parallaxPrototype.LayersLQ, cancel)
                );
            }

            _loadingParallaxes.Remove(name, out _);

            if (token.Token.IsCancellationRequested) return;

            _parallaxesLQ[name] = layers[1];
            _parallaxesHQ[name] = layers[0];

        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to loaded parallax {name}: {ex}");
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

