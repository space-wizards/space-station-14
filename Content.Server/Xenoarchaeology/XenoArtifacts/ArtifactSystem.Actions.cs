using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public partial class ArtifactSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    [ValidatePrototypeId<EntityPrototype>] private const string ArtifactActivateActionId = "ActionArtifactActivate";

    /// <summary>
    ///     Used to add the artifact activation action (hehe), which lets sentient artifacts activate themselves,
    ///     either through admemery or the sentience effect.
    /// </summary>
    public void InitializeActions()
    {
        SubscribeLocalEvent<ArtifactComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ArtifactComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ArtifactComponent, ArtifactSelfActivateEvent>(OnSelfActivate);
    }

    private void OnMapInit(EntityUid uid, ArtifactComponent component, MapInitEvent args)
    {
        RandomizeArtifact(uid, component);
        _actions.AddAction(uid, ref component.ActivateActionEntity, ArtifactActivateActionId);
    }

    private void OnRemove(EntityUid uid, ArtifactComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ActivateActionEntity);
    }

    private void OnSelfActivate(EntityUid uid, ArtifactComponent component, ArtifactSelfActivateEvent args)
    {
        if (component.CurrentNodeId == null)
            return;

        var curNode = GetNodeFromId(component.CurrentNodeId.Value, component).Id;
        _popup.PopupEntity(Loc.GetString("activate-artifact-popup-self", ("node", curNode)), uid, uid);
        TryActivateArtifact(uid, uid, component);

        args.Handled = true;
    }
}
