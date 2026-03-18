using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Client.Parallax.Data;
using Content.Shared.CCVar;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;

namespace Content.Client.Parallax.Managers;

public sealed class ParallaxManager : IParallaxManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IDependencyCollection _deps = null!;

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
            _sawmill.Debug($"Cancelling loading parallax {name}");
            loading.Cancel();
            _loadingParallaxes.Remove(name, out _);
            return;
        }

        _sawmill.Debug($"Unloading parallax {name}");

        if (_parallaxesLQ.Remove(name, out var layers))
        {
            foreach (var layer in layers)
            {
                layer.Config.Texture.Unload(_deps);
            }
        }

        if (_parallaxesHQ.Remove(name, out layers))
        {
            foreach (var layer in layers)
            {
                layer.Config.Texture.Unload(_deps);
            }
        }
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

        // Keep a list of layers we did successfully load, in case we have to cancel the load.
        var loadedLayers = new List<ParallaxLayerPrepared>();

        try
        {
            var parallaxPrototype = _prototypeManager.Index<ParallaxPrototype>(name);

            ParallaxLayerPrepared[][] layers;

            if (parallaxPrototype.LayersLQUseHQ)
            {
                layers = new ParallaxLayerPrepared[2][];
                layers[0] = layers[1] = await LoadParallaxLayers(parallaxPrototype.Layers, loadedLayers, cancel);
            }
            else
            {
                // Explicitly allocate params array to avoid sandbox violation since C# 14.
                var tasks = new[]
                {
                    LoadParallaxLayers(parallaxPrototype.Layers, loadedLayers, cancel),
                    LoadParallaxLayers(parallaxPrototype.LayersLQ, loadedLayers, cancel),
                };
                layers = await Task.WhenAll(tasks);
            }

            cancel.ThrowIfCancellationRequested();

            _loadingParallaxes.Remove(name);

            _parallaxesLQ[name] = layers[1];
            _parallaxesHQ[name] = layers[0];

            _sawmill.Verbose($"Loading parallax {name} completed");
        }
        catch (OperationCanceledException)
        {
            _sawmill.Verbose($"Loading parallax {name} cancelled");

            foreach (var loadedLayer in loadedLayers)
            {
                loadedLayer.Config.Texture.Unload(_deps);
            }
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to loaded parallax {name}: {ex}");
        }
    }

    private async Task<ParallaxLayerPrepared[]> LoadParallaxLayers(
        List<ParallaxLayerConfig> layersIn,
        List<ParallaxLayerPrepared> loadedLayers,
        CancellationToken cancel = default)
    {
        // Because this is async, make sure it doesn't change (prototype reloads could muck this up)
        // Since the tasks aren't awaited until the end, this should be fine
        var tasks = new Task<ParallaxLayerPrepared>[layersIn.Count];
        for (var i = 0; i < layersIn.Count; i++)
        {
            tasks[i] = LoadParallaxLayer(layersIn[i], loadedLayers, cancel);
        }
        return await Task.WhenAll(tasks);
    }

    private async Task<ParallaxLayerPrepared> LoadParallaxLayer(
        ParallaxLayerConfig config,
        List<ParallaxLayerPrepared> loadedLayers,
        CancellationToken cancel = default)
    {
        var prepared = new ParallaxLayerPrepared()
        {
            Texture = await config.Texture.GenerateTexture(cancel),
            Config = config
        };

        loadedLayers.Add(prepared);

        return prepared;
    }
}

