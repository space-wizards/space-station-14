using Content.Shared.Light.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Smoking;

namespace Content.Shared.Light.EntitySystems;

public sealed class SharedMatchboxSystem : EntitySystem
{
    [Dependency] private readonly SharedMatchstickSystem _stickSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MatchboxComponent, InteractUsingEvent>(OnInteractUsing, before: new[] { typeof(SharedStorageSystem) });
    }

    private void OnInteractUsing(EntityUid uid, MatchboxComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !EntityManager.TryGetComponent(args.Used, out MatchstickComponent? matchstick))
            return;

        if (matchstick.CurrentState == SmokableState.Unlit)
            _stickSystem.Ignite((args.Used, matchstick), args.User);

        args.Handled = true;
    }
}
