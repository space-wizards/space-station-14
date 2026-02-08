using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Instruments;
using Content.Server.Store.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Kitchen;
using Content.Shared.PAI;
using Content.Shared.Popups;
using Content.Shared.Instruments;
using Content.Shared.Store;
using Content.Shared.SubFloor;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Server.PAI;

public sealed class PAISystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedTrayScannerSystem _trayScannerSystem = default!;
    [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ToggleableGhostRoleSystem _toggleableGhostRole = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    /// <summary>
    /// Possible symbols that can be part of a scrambled pai's name.
    /// </summary>
    private static readonly char[] SYMBOLS = new[] { '#', '~', '-', '@', '&', '^', '%', '$', '*', ' ' };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PAIComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<PAIComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<PAIComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
        SubscribeLocalEvent<PAIComponent, PAIAccessChangedEvent>(OnPAIAccessChanged);
        SubscribeLocalEvent<TryGetIdentityShortInfoEvent>(OnTryGetIdentityShortInfo);
    }

    // since pAIs don't have an ID card themselves, we use their name for anything that looks for name through ID card.
    private void OnTryGetIdentityShortInfo(TryGetIdentityShortInfoEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PAIComponent>(args.ForActor, out var pai))
            return;

        args.Title = Name(args.ForActor);
        args.Handled = true;
    }

    // pAI's access is determined by the access of the ID card that is with the pAI inside a PDA.
    private void OnPAIAccessChanged(EntityUid uid, PAIComponent component, PAIAccessChangedEvent args)
    {
        foreach (var actor in _uiSystem.GetActors(uid, StoreUiKey.Key))
        {
            _storeSystem.UpdateUserInterface(actor, uid);
        }

        if (!TryComp<ActionsComponent>(uid, out var actions))
            return;

        var accessTags = _accessReader.FindAccessTags(uid);
        TryComp<TrayScannerComponent>(uid, out var trayScanner);

        foreach (var actionId in actions.Actions)
        {
            if (!TryComp<ActionComponent>(actionId, out var action))
                continue;

            var actionEvent = _actionsSystem.GetEvent(actionId);

            if (!TryComp<ActionAccessRequirementComponent>(actionId, out var requirement))
                continue;

            var denied = requirement.Blacklist != null && accessTags.Any(tag => requirement.Blacklist.Contains(tag)) ||
                         requirement.Whitelist != null && !accessTags.Any(tag => requirement.Whitelist.Contains(tag));

            if (denied)
            {
                if (actionEvent is TrayScannerActionEvent && trayScanner is { Enabled: true })
                {
                    _trayScannerSystem.SetScannerEnabled(uid, false, trayScanner);
                    _actionsSystem.SetToggled((actionId, action), false);
                }

                if (actionEvent is OpenUiActionEvent openUi && openUi.Key != null)
                {
                    _uiSystem.CloseUi(uid, openUi.Key);
                }
            }
        }
    }

    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        if (!TryComp<PAIComponent>(ev.StoreUid, out var component))
            return;

        if (ev.PurchasedItem.ProductAction != null)
        {
            component.PurchasedAbilities.Add(ev.PurchasedItem.ProductAction.Value);
        }
    }

    private void OnUseInHand(EntityUid uid, PAIComponent component, UseInHandEvent args)
    {
        // Not checking for Handled because ToggleableGhostRoleSystem already marks it as such.

        if (!TryComp<MindContainerComponent>(uid, out var mind) || !mind.HasMind)
            component.LastUser = args.User;
    }

    private void OnMindAdded(EntityUid uid, PAIComponent component, MindAddedMessage args)
    {
        var existingActions = new HashSet<string>();
        foreach (var actionEnt in _actionsSystem.GetActions(uid))
        {
            if (TryComp(actionEnt.Owner, out MetaDataComponent? metaData) && metaData.EntityPrototype != null)
            {
                existingActions.Add(metaData.EntityPrototype.ID);
            }
        }

        foreach (var action in component.PurchasedAbilities)
        {
            if (!existingActions.Contains(action.Id))
            {
                _actionsSystem.AddAction(uid, action);
                existingActions.Add(action.Id);
            }
        }

        if (component.LastUser == null)
            return;

        // Ownership tag
        var val = Loc.GetString("pai-system-pai-name", ("owner", component.LastUser));

        // TODO Identity? People shouldn't dox-themselves by carrying around a PAI.
        // But having the pda's name permanently be "old lady's PAI" is weird.
        // Changing the PAI's identity in a way that ties it to the owner's identity also seems weird.
        // Cause then you could remotely figure out information about the owner's equipped items.

        _metaData.SetEntityName(uid, val);
    }

    private void OnMindRemoved(EntityUid uid, PAIComponent component, MindRemovedMessage args)
    {
        // Mind was removed, shutdown the PAI.
        PAITurningOff(uid);
    }

    private void OnMicrowaved(EntityUid uid, PAIComponent comp, BeingMicrowavedEvent args)
    {
        // name will always be scrambled whether it gets bricked or not, this is the reward
        ScrambleName(uid, comp);

        // randomly brick it
        if (_random.Prob(comp.BrickChance))
        {
            _popup.PopupEntity(Loc.GetString(comp.BrickPopup), uid, PopupType.LargeCaution);
            _toggleableGhostRole.Wipe(uid);
            RemComp<PAIComponent>(uid);
            RemComp<ToggleableGhostRoleComponent>(uid);
        }
        else
        {
            // you are lucky...
            _popup.PopupEntity(Loc.GetString(comp.ScramblePopup), uid, PopupType.Large);
        }
    }

    private void ScrambleName(EntityUid uid, PAIComponent comp)
    {
        // create a new random name
        var len = _random.Next(6, 18);
        var name = new StringBuilder(len);
        for (int i = 0; i < len; i++)
        {
            name.Append(_random.Pick(SYMBOLS));
        }

        // add 's pAI to the scrambled name
        var val = Loc.GetString("pai-system-pai-name-raw", ("name", name.ToString()));
        _metaData.SetEntityName(uid, val);
    }
    public void PAITurningOff(EntityUid uid)
    {
        //  Close the instrument interface if it was open
        //  before closing
        if (HasComp<ActiveInstrumentComponent>(uid))
        {
            _instrumentSystem.ToggleInstrumentUi(uid, uid);
        }

        //  Stop instrument
        if (TryComp<InstrumentComponent>(uid, out var instrument))
            _instrumentSystem.Clean(uid, instrument);

        if (TryComp(uid, out MetaDataComponent? metadata))
        {
            var proto = metadata.EntityPrototype;
            if (proto != null)
                _metaData.SetEntityName(uid, proto.Name);
        }
    }
}
