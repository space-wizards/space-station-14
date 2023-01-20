using Content.Shared.Standing;
using Robust.Shared.Timing;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBuckle();
        InitializeStrap();
    }
}
