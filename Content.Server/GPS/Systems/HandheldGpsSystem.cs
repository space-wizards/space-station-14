using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Tools.Components;

namespace Content.Server.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldGpsComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, HandheldGpsComponent component, ActivateInWorldEvent args)
    {
        // if it has a delay component, try delaying, and return if it's already delaying
        if (TryComp(uid, out UseDelayComponent? useDelay)
            && !_useDelaySystem.TryResetDelay(uid, true, useDelay))
        {
            return;
        }

        component.DisplayMode = !component.DisplayMode;
        Dirty(uid, component);
    }
}

