using Content.Shared.Actions;
using Content.Shared.Lightning.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Lightning;

public abstract class SharedLightningSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<SharedLightningComponent, LightningEvent>(OnLightning);
    }

    //TODO: Add way to arc/chain lightning

    private void OnLightning(EntityUid uid, SharedLightningComponent component, LightningEvent ev)
    {
        //TODO: Figure out the best way to handle spawning on lightning event in shared, if needed.
        //Issue with this is I can't access rotation otherwise it's fine.
    }
}
