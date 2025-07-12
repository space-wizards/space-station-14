using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Interaction;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Popups;

namespace Content.Shared.Xenoarchaeology.Equipment;

public abstract class SharedArtifactNukerSystem : EntitySystem
{
    //Disabled because my IDE don't likes protected "_systems"
#pragma warning disable IDE1006
    [Dependency] protected readonly SharedXenoArtifactSystem _xenoSys = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactNukerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
    }

    public void OnBeforeRangedInteract(Entity<ArtifactNukerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Target is not null && args.CanReach && !args.Handled)
        {
            if (!TryComp<XenoArtifactComponent>(args.Target, out var comp))
            {
                _popup.PopupClient(Loc.GetString(ent.Comp.PopupNotArtifact), args.User);
                args.Handled = true;
                return;
            }

            var xenoComp = (args.Target.Value, comp);
            if (_xenoSys.GetActiveNodes(xenoComp) is [])
            {
                _popup.PopupClient(Loc.GetString(ent.Comp.PopupZeroNodes), args.User);
                args.Handled = true;
                return;
            }

            var refEvent = new AttemptNukeArtifact(ent.Comp, args.User);
            RaiseLocalEvent(args.Target.Value, ref refEvent);
            args.Handled = true;
        }
    }
}

[ByRefEvent] public record struct AttemptNukeArtifact(ArtifactNukerComponent Nuker, EntityUid User);
