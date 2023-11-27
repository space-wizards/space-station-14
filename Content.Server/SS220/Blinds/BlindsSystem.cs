// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction;
using Content.Shared.SS220.Blinds;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Enumerators;

namespace Content.Server.SS220.Blinds;

public sealed class BlindsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private const int MaxConnectedBlinds = 64;

    // <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BlindsComponent, ActivateInWorldEvent>(OnActivated);
    }

    public void SetOpen(EntityUid uid, BlindsComponent component, bool state)
    {
        component.IsOpen = state;
        UpdateState(uid, component);
    }

    private void TrySetOpenAnchoredEntities(bool state, AnchoredEntitiesEnumerator entities, HashSet<EntityUid> processedEntities)
    {
        while (entities.MoveNext(out var entity))
        {
            if (processedEntities.Contains(entity.Value))
                continue;

            TrySetOpenAllConnected(entity.Value, state, processedEntities);
        }
    }

    public void TrySetOpenAllConnected(EntityUid uid, bool state, HashSet<EntityUid>? processedEntities = null)
    {
        // No lagging the server with a shitton of connected blinds
        if (processedEntities is not null && processedEntities.Count >= MaxConnectedBlinds)
            return;

        if (!TryComp<BlindsComponent>(uid, out var component))
            return;

        SetOpen(uid, component, state);

        processedEntities ??= new HashSet<EntityUid>();
        processedEntities.Add(uid);

        // Make connected blinds change their state as well
        if (!TryComp<TransformComponent>(uid, out var transform))
            return;

        if (transform.Anchored && _mapManager.TryGetGrid(transform.GridUid, out var grid))
        {
            var pos = grid.CoordinatesToTile(transform.Coordinates);

            TrySetOpenAnchoredEntities(component.IsOpen, grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(1, 0)), processedEntities);
            TrySetOpenAnchoredEntities(component.IsOpen, grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(-1, 0)), processedEntities);
            TrySetOpenAnchoredEntities(component.IsOpen, grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(0, 1)), processedEntities);
            TrySetOpenAnchoredEntities(component.IsOpen, grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(0, -1)), processedEntities);
        }
    }

    private void OnInit(EntityUid uid, BlindsComponent component, ComponentInit args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, BlindsComponent component)
    {
        _appearance.SetData(uid, BlindsVisualState.State, component.IsOpen);
        if (TryComp<OccluderComponent>(uid, out var occluder))
            _occluder.SetEnabled(uid, !component.IsOpen, occluder);
    }

    private void OnActivated(EntityUid uid, BlindsComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        TrySetOpenAllConnected(uid, !component.IsOpen);
        var soundToPlay = component.IsOpen ? component.OpenSound : component.CloseSound;
        _audio.PlayPvs(soundToPlay, args.User);

        args.Handled = true;
    }
}
