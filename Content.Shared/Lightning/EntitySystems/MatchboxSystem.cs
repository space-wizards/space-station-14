using Content.Shared.Light.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Shared.Light.EntitySystems;

public sealed class MatchboxSystem : EntitySystem
{
    [Dependency] private readonly SharedMatchstickSystem _match = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MatchboxComponent, InteractUsingEvent>(OnInteractUsing, before: [ typeof(SharedStorageSystem) ]);
    }

    private void OnInteractUsing(Entity<MatchboxComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<MatchstickComponent>(args.Used, out var matchstick))
            return;

        args.Handled = _match.TryIgnite((args.Used, matchstick), args.User);
    }
}
