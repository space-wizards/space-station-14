using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Reflection;

namespace Content.Client.Options;

/// <summary>
/// Implements <see cref="OptionsVisualizerComponent"/>.
/// </summary>
public sealed class OptionsVisualizerSystem : EntitySystem
{
    private static readonly (OptionVisualizerOptions, CVarDef<bool>)[] OptionVars =
    {
        (OptionVisualizerOptions.Test, CCVars.DebugOptionVisualizerTest),
        (OptionVisualizerOptions.ReducedMotion, CCVars.ReducedMotion),
    };

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private OptionVisualizerOptions _currentOptions;

    public override void Initialize()
    {
        base.Initialize();

        foreach (var (_, cvar) in OptionVars)
        {
            Subs.CVar(_cfg, cvar, _ => CVarChanged());
        }

        UpdateActiveOptions();

        SubscribeLocalEvent<OptionsVisualizerComponent, ComponentStartup>(OnComponentStartup);
    }

    private void CVarChanged()
    {
        UpdateActiveOptions();
        UpdateAllComponents();
    }

    private void UpdateActiveOptions()
    {
        _currentOptions = OptionVisualizerOptions.Default;

        foreach (var (value, cVar) in OptionVars)
        {
            if (_cfg.GetCVar(cVar))
                _currentOptions |= value;
        }
    }

    private void UpdateAllComponents()
    {
        var query = EntityQueryEnumerator<OptionsVisualizerComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var component, out var sprite))
        {
            UpdateComponent(uid, component, sprite);
        }
    }

    private void OnComponentStartup(EntityUid uid, OptionsVisualizerComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        UpdateComponent(uid, component, sprite);
    }

    private void UpdateComponent(EntityUid uid, OptionsVisualizerComponent component, SpriteComponent sprite)
    {
        foreach (var (layerKeyRaw, layerData) in component.Visuals)
        {
            OptionsVisualizerComponent.LayerDatum? matchedDatum = null;
            foreach (var datum in layerData)
            {
                if ((datum.Options & _currentOptions) != datum.Options)
                    continue;

                matchedDatum = datum;
            }

            if (matchedDatum == null)
                continue;

            var layerIndex = _reflection.TryParseEnumReference(layerKeyRaw, out var @enum)
                ? _sprite.LayerMapReserve((uid, sprite), @enum)
                : _sprite.LayerMapReserve((uid, sprite), layerKeyRaw);

            _sprite.LayerSetData((uid, sprite), layerIndex, matchedDatum.Data);
        }
    }
}

