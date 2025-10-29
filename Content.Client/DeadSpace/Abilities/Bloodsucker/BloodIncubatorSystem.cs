using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Robust.Shared.GameStates;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Abilities.Bloodsucker;

public sealed partial class BloodIncubatorSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodIncubatorComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, BloodIncubatorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BloodIncubatorComponentState state)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (state.State < 0 || state.State >= component.States.Count)
            return;

        var stateName = component.States[state.State];

        _sprite.LayerSetRsiState((uid, sprite), 0, stateName);
    }

}
