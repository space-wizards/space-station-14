using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Localizations;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;


namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires user access
/// This just handles the trigger and emag interactions, <see cref="AccessReaderComponent"/> handles accesses
/// </summary>
public sealed partial class XATAccessSystem : BaseXATSystem<XATAccessComponent>
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedXenoArtifactSystem _xenoarch = default!;
    public override void Initialize()
    {
        base.Initialize();
        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<GotEmaggedEvent>(OnNodeEmagged);
        XATSubscribeDirectEvent<ExaminedEvent>(OnExamine);
    }

    /// summary>
    /// Randomly choose access from list and add it to reader
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStartup(Entity<XATAccessComponent> ent, ref MapInitEvent args)
    {
        SetNodeAccess(ent);
    }

    /// <summary>
    /// Check whether there is a node requiring access, if there is then handle the zapping of the emag.
    /// We also raise the GotEmaggedEvent here onto other nodes because otherwise we're subscribing twice
    /// Which is why this isn't in the list of relayed events
    /// </summary>
    [SubscribeLocalEvent]
    private void OnEmagged(Entity<XenoArtifactComponent> ent, ref GotEmaggedEvent args)
    {
        var nodes = _xenoarch.GetAllNodes(ent);
        var ev = new XenoArchNodeRelayedEvent<GotEmaggedEvent>(ent, args);

        foreach (var node in nodes) // here we make sure all nodes get the event
            RaiseLocalEvent(node, ref ev);

        foreach (var node in nodes) //but here we only need to care about zapping once, so here we just stop once it's found
        {
            if (HasComp<XATAccessComponent>(node.Owner))
            {
                args.Handled = true;
                args.Repeatable = true;
                return;
            }
        }
    }

    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (!HasComp<AccessComponent>(args.Used)) //ONLY check the used item, you tap your ID to it.
            return;

        if (CheckAccess(args.Used, artifact, (node.Owner, node.Comp1)))
        {
            _audio.PlayPredicted(node.Comp1.AccessSound, args.Used, args.User);
            Trigger(artifact, node);
        }
        else
        {
            if (node.Comp1.WrongAccessPopup != null)
                _popup.PopupEntity(Loc.GetString(node.Comp1.WrongAccessPopup), args.Used, args.User);
            _audio.PlayPredicted(node.Comp1.DeniedSound, args.Used, args.User);
        }
    }

    private void OnNodeEmagged(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref GotEmaggedEvent args)
    {
        Trigger(artifact, node); // zap
    }

    /// <summary>
    /// Examination text. Could raise examined event again to go to the access reader but has misprediction issues.
    /// </summary>
    private void OnExamine(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        if (!TryComp<AccessReaderComponent>(node, out var accessComp))
            return;

        var localizedNames = _access.GetLocalizedAccessNames(accessComp.AccessLists);

        // If the string list is empty either there were no access restrictions or the localized names were invalid
        if (localizedNames.Count == 0)
            return;

        var accessesFormatted = ContentLocalizationManager.FormatListToOr(localizedNames);
        var settingsMessage = Loc.GetString(node.Comp1.ExamineString, ("access", accessesFormatted));
        args.PushMarkup(settingsMessage);
    }

    /// <summary>
    /// Set Accesses
    /// </summary>
    private void SetNodeAccess(Entity<XATAccessComponent> ent)
    {
        if (ent.Comp.PotentialAccess == null || !TryComp<AccessReaderComponent>(ent, out var accessComp)) // undefined, stop here.
            return;

        var access = ent.Comp.PotentialAccess.ElementAt(_random.Next(ent.Comp.PotentialAccess.Count)); //get random access from hashset.
        if (_proto.Index(access) == null) // invalid access, stop here.
            return;

        _access.ReplaceOriginalAccess((ent.Owner, accessComp), new List<ProtoId<AccessLevelPrototype>>() { access }); //retcon the current access, it was always there, see?
    }

    /// <summary>
    /// Read access from interaction
    /// </summary>
    /// <returns> true if appropriate access </returns>
    private bool CheckAccess(EntityUid user, EntityUid used, Entity<XATAccessComponent> node)
    {
        if (!TryComp<AccessReaderComponent>(node.Owner, out var accessComp)) //the access trigger should have an AccessReaderComponent alongside it
            return false;

        if (_access.IsAllowed(user, node.Owner, accessComp))
            return true;
        else
            return false;
    }
}
