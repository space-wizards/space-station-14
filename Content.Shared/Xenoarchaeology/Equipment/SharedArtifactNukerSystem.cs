using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
///     Logic for the <see cref="SharedArtifactNukerSystem"/>.
///     For the proper prediction it splited into 3 parts:
///     this one, Server and Client.
/// </summary>
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
        SubscribeLocalEvent<ArtifactNukerComponent, ArtifactNukerIndexChangeMessage>(OnIndexChange);
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

            if (ent.Comp.Index is null)
            {
                _popup.PopupClient(Loc.GetString("artifact-nuker-popup-noindex"), args.User);
                args.Handled = true;
                return;
            }

            var refEvent = new AttemptNukeArtifact(ent, args.User, ent.Comp.Index.Value);
            RaiseLocalEvent(args.Target.Value, ref refEvent);
            _popup.PopupClient(Loc.GetString("artifact-nuker-popup-success"), args.User, PopupType.Medium);
            args.Handled = true;
        }
    }

    public void OnIndexChange(Entity<ArtifactNukerComponent> ent, ref ArtifactNukerIndexChangeMessage args)
    {
        ent.Comp.Index = args.Index;
        Dirty(ent);
    }
}

#region events and messages

[ByRefEvent]
public record struct AttemptNukeArtifact(Entity<ArtifactNukerComponent> Nuker, EntityUid User, int index);

[Serializable, NetSerializable]
public sealed class ArtifactNukerIndexChangeMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}

#endregion
