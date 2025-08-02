using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
///     Logic for the <see cref="SharedArtifactNukerSystem"/>.
///     For the prediction it splited into 3 parts:
///     this one, Server and Client.
///     Feel free to kill server and client systems and leave only this one when powercells and random will be predicted.
/// </summary>
public abstract class SharedArtifactNukerSystem : EntitySystem
{
    //Disabled because my IDE don't likes protected "_systems"
#pragma warning disable IDE1006
    [Dependency] protected readonly SharedXenoArtifactSystem _xenoSys = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
#pragma warning restore IDE1006

    /// <inheritdoc/>
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
                _popup.PopupClient(Loc.GetString("artifact-nuker-popup-notartifact"), args.User);
                args.Handled = true;
                return;
            }

            var xenoComp = (args.Target.Value, comp);
            if (_xenoSys.GetActiveNodes(xenoComp) is [])
            {
                _popup.PopupClient(Loc.GetString("artifact-nuker-popup-zeronodes"), args.User);
                args.Handled = true;
                return;
            }

            if (TryComp<UseDelayComponent>(ent, out var delay))
                _useDelay.TryResetDelay((ent, delay), true);

            var refEvent = new AttemptNukeArtifact(ent, args.User);
            RaiseLocalEvent(args.Target.Value, ref refEvent);
            _popup.PopupClient(Loc.GetString("artifact-nuker-popup-success"), args.User, PopupType.Medium);
            args.Handled = true;
        }
    }
}

#region Events

[ByRefEvent]
public record struct AttemptNukeArtifact(Entity<ArtifactNukerComponent> Nuker, EntityUid User);

#endregion
