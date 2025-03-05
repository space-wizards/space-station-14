using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using System.Numerics;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosPipeLayersSystem : SharedAtmosPipeLayersSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosPipeAppearanceSystem _pipeAppearance = default!;

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

        if (ent.Comp.OffsetAboveFloorLayers &&
            _appearance.TryGetData<Vector2>(ent, SubfloorLayers.FirstLayer, out var offset))
        {
            sprite.LayerSetOffset(SubfloorLayers.FirstLayer, offset);
        }
    }
}
