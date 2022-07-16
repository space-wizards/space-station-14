using System.Collections.Concurrent;
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

    private ISawmill _sawmill = Logger.GetSawmill("parallax");

    public Vector2 ParallaxAnchor { get; set; }

    private readonly ConcurrentDictionary<string, ParallaxLayerPrepared[]> _parallaxesLQ = new();
    private readonly ConcurrentDictionary<string, ParallaxLayerPrepared[]> _parallaxesHQ = new();

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _loadingParallaxes = new();

    public ParallaxLayerPrepared[] GetParallaxLayers(string name)
    {
        return _configurationManager.GetCVar(CCVars.ParallaxLowQuality) ? _parallaxesLQ[name] : _parallaxesHQ[name];
    }

    public async void LoadDefaultParallax()
    {
        await LoadParallaxByName("Default");
    }

    private async Task LoadParallaxByName(string name)
    {
        if (_parallaxesLQ.ContainsKey(name) || _loadingParallaxes.ContainsKey(name)) return;

        // Cancel any existing load and setup the new cancellation token
        var token = new CancellationTokenSource();
        _loadingParallaxes[name] = token;
        var cancel = token.Token;

        // Begin (for real)
        _sawmill.Info($"Loading parallax {name}");

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

            _parallaxesLQ[name] = lq;
            _parallaxesHQ[name] = hq;

            _sawmill.Info($"Loaded parallax {name}");

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

