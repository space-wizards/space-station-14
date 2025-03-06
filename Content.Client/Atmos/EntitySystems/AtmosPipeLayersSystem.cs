using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;
using System.Numerics;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// The system responsible for updating the appearance of layered gas pipe
/// </summary>
public sealed partial class AtmosPipeLayersSystem : SharedAtmosPipeLayersSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosPipeAppearanceSystem _pipeAppearance = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<AtmosPipeLayersComponent> ent, ref AppearanceChangeEvent ev)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_appearance.TryGetData<string>(ent, PipeVisualLayers.Pipe, out var pipeState))
        {
            sprite.LayerSetState(PipeVisualLayers.Pipe, pipeState);
        }

        if (TryComp<PipeAppearanceComponent>(ent, out var pipeAppearance) &&
            _appearance.TryGetData<string>(ent, PipeVisualLayers.Connector, out var connectorState))
        {
            _pipeAppearance.SetLayerState((ent, pipeAppearance), connectorState);
        }

        if (_appearance.TryGetData<Vector2>(ent, PipeVisualLayers.Device, out var offset))
        {
            foreach (var layer in ent.Comp.LayersToOffset)
                sprite.LayerSetOffset(ParseKey(layer), offset);
        }
    }

    /// <summary>
    /// Parses a string for enum references
    /// </summary>
    /// <param name="keyString">The string to parse</param>
    /// <returns>The parsed string</returns>
    private object ParseKey(string keyString)
    {
        if (_reflection.TryParseEnumReference(keyString, out var @enum))
            return @enum;

        return keyString;
    }
}
