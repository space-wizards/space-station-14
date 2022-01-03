using Content.Server.Xenoarchaeology.XenoArtifacts.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Systems;

public class ArtifactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, ArtifactComponent component, InteractHandEvent args)
    {
        TryActivateArtifact(uid, component);
    }

    public bool TryActivateArtifact(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // check if artifact isn't under cooldown
        var timeDif = _gameTiming.CurTime - component.LastActivationTime;
        if (timeDif.TotalSeconds < component.CooldownTime)
            return false;

        ForceActivateArtifact(uid, component);
        return true;
    }

    public void ForceActivateArtifact(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastActivationTime = _gameTiming.CurTime;
        RaiseLocalEvent(uid, new ArtifactActivatedEvent());
    }
}
