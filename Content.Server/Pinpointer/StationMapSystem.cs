using Content.Shared.Interaction.Events;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;

namespace Content.Server.Pinpointer;

public sealed class StationMapSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationMapComponent, UseInHandEvent>(OnMapUse);
    }

    private void OnMapUse(EntityUid uid, StationMapComponent component, UseInHandEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.TryToggleUi(uid, StationMapUiKey.Key, actor.PlayerSession);
    }
}
