using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public partial class ArtifactSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <summary>
    ///     Used to add the artifact activation action (hehe), which lets sentient artifacts activate themselves,
    ///     either through admemery or the sentience effect.
    /// </summary>
    public void InitializeActions()
    {
        SubscribeLocalEvent<ArtifactComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ArtifactComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ArtifactComponent, ArtifactSelfActivateEvent>(OnSelfActivate);
    }

    private void OnStartup(EntityUid uid, ArtifactComponent component, ComponentStartup args)
    {
        if (_prototype.TryIndex<InstantActionPrototype>("ArtifactActivate", out var proto))
        {
            _actions.AddAction(uid, new InstantAction(proto), null);
        }
    }

    private void OnRemove(EntityUid uid, ArtifactComponent component, ComponentRemove args)
    {
        if (_prototype.TryIndex<InstantActionPrototype>("ArtifactActivate", out var proto))
        {
            _actions.RemoveAction(uid, new InstantAction(proto));
        }
    }

    private void OnSelfActivate(EntityUid uid, ArtifactComponent component, ArtifactSelfActivateEvent args)
    {
        if (component.CurrentNode == null)
            return;

        var curNode = component.CurrentNode.Id;
        _popup.PopupEntity(Loc.GetString("activate-artifact-popup-self", ("node", curNode)), uid, uid);
        TryActivateArtifact(uid, uid, component);

        args.Handled = true;
    }
}

