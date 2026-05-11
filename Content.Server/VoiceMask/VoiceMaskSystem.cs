using Content.Server.Speech;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.VoiceMask;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IdentitySystem _identity = default!;

    /// <summary>
    ///  The name of the client-side type that represents the user interface window.
    ///  Used for innate voice masks, which need to be able to create their own UIs.
    /// </summary>
    private const string UiGeneratedName = "VoiceMaskBoundUserInterface";

    // CCVar.
    private int _maxNameLength;

    public override void Initialize()
    {
        base.Initialize();

        // These events should fire in the order Innate -> Implant -> Inventory
        // Transform speaker name events
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerNameInventory);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerNameImplant);
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerNameEvent>(OnInnateTransformSpeakerName);

        // See identity attempt events
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<SeeIdentityAttemptEvent>>(OnSeeIdentityAttemptEvent);
        SubscribeLocalEvent<VoiceMaskComponent, SeeIdentityAttemptEvent>(OnInnateSeeIdentityAttemptEvent);

        // Transform speech events
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeechEvent>>(OnTransformSpeechInventory, before: [typeof(AccentSystem)]);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<TransformSpeechEvent>>(OnTransformSpeechImplant, before: [typeof(AccentSystem)]);
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeechEvent>(OnTransformSpeech, before: [typeof(AccentSystem)]);

        // Voice mask transform things
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<VoiceMaskToggledEvent>>((ent, ref ev) => OnVoiceMaskToggledEvent(ent, ref ev.Args));
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<VoiceMaskToggledEvent>>((ent, ref ev) => OnVoiceMaskToggledEvent(ent, ref ev.Args));
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskToggledEvent>(OnVoiceMaskToggledEvent);

        // Other events
        SubscribeLocalEvent<VoiceMaskComponent, ImplantImplantedEvent>(OnImplantImplantedEvent);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRemovedEvent>(OnImplantRemovedEventEvent);
        SubscribeLocalEvent<VoiceMaskComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeNameMessage>(OnChangeName);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVerbMessage>(OnChangeVerb);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskToggleMessage>(OnToggle);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskAccentToggleMessage>(OnAccentToggle);
        SubscribeLocalEvent<VoiceMaskComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<VoiceMaskSetNameEvent>(OpenUI);
        SubscribeLocalEvent<VoiceMaskComponent, MapInitEvent>(OnMapInit);

        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    private void OnMapInit(Entity<VoiceMaskComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.IsInnate)
            return;

        // all masks should be inactive on creation
        ent.Comp.Active = false;

        _actions.AddAction(ent, ent.Comp.Action);
        _uiSystem.SetUi((ent, null), VoiceMaskUIKey.Key, new InterfaceData(UiGeneratedName));
        _identity.QueueIdentityUpdate(ent.Owner);
    }

    /// <summary>
    ///  Toggles this mask off it it isn't the mask turned on
    /// </summary>
    private void OnVoiceMaskToggledEvent(Entity<VoiceMaskComponent> ent, ref VoiceMaskToggledEvent args)
    {
        // we only toggle when the other mask turns on
        if (!args.Active)
            return;

        // we don't want the entity turned on to be turned off, and there isn't any work to do if it already inactive
        if (ent.Owner == args.Mask || !ent.Comp.Active)
            return;

        // turn it off
        ent.Comp.Active = false;

        // update the
        UpdateUI(ent);
        _identity.QueueIdentityUpdate(args.Source);
    }

    /// <summary>
    ///     Hides accent if the voice mask is on and the option to block accents is on
    /// </summary>
    private void TransformSpeech(Entity<VoiceMaskComponent> entity, TransformSpeechEvent args)
    {
        if (entity.Comp.AccentHide && entity.Comp.Active)
            args.Cancel();
    }

    private void OnTransformSpeech(Entity<VoiceMaskComponent> entity, ref TransformSpeechEvent args)
    {
        TransformSpeech(entity, args);
    }

    private void OnTransformSpeechInventory(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<TransformSpeechEvent> args)
    {
        TransformSpeech(entity, args.Args);
    }

    private void OnTransformSpeechImplant(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<TransformSpeechEvent> args)
    {
        TransformSpeech(entity, args.Args);
    }

    private void OnInnateTransformSpeakerName(Entity<VoiceMaskComponent> ent, ref TransformSpeakerNameEvent args)
    {
        TransformVoice(ent, args);
    }

    private void OnTransformSpeakerNameInventory(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        TransformVoice(entity, args.Args);
    }

    private void OnTransformSpeakerNameImplant(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<TransformSpeakerNameEvent> args)
    {
        TransformVoice(entity, args.Args);
    }

    private void OnInnateSeeIdentityAttemptEvent(Entity<VoiceMaskComponent> entity, ref SeeIdentityAttemptEvent args)
    {
        if (!entity.Comp.OverrideIdentity || !entity.Comp.Active || !entity.Comp.IsInnate)
            return;

        args.NameOverride = GetCurrentVoiceName(entity);
    }

    private void OnSeeIdentityAttemptEvent(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<SeeIdentityAttemptEvent> args)
    {
        if (!entity.Comp.OverrideIdentity || !entity.Comp.Active)
            return;

        args.Args.NameOverride = GetCurrentVoiceName(entity);
    }

    private void OnImplantImplantedEvent(Entity<VoiceMaskComponent> entity, ref ImplantImplantedEvent ev)
    {
        entity.Comp.Active = false;
        _identity.QueueIdentityUpdate(ev.Implanted);
    }

    private void OnImplantRemovedEventEvent(Entity<VoiceMaskComponent> entity, ref ImplantRemovedEvent ev)
    {
        _identity.QueueIdentityUpdate(ev.Implanted);
    }

    private void OnLockToggled(Entity<VoiceMaskComponent> ent, ref LockToggledEvent args)
    {
        if (args.Locked)
            _actions.RemoveAction(ent.Comp.ActionEntity);
        else if (_container.TryGetContainingContainer(ent.Owner, out var container))
            _actions.AddAction(container.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    #region User inputs from UI
    private void OnChangeVerb(Entity<VoiceMaskComponent> entity, ref VoiceMaskChangeVerbMessage msg)
    {
        if (msg.Verb is { } id && !_proto.HasIndex<SpeechVerbPrototype>(id))
            return;

        entity.Comp.VoiceMaskSpeechVerb = msg.Verb;
        // verb is only important to metagamers so no need to log as opposed to name

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-success"), entity, msg.Actor);

        UpdateUI(entity);
    }

    private void OnChangeName(Entity<VoiceMaskComponent> entity, ref VoiceMaskChangeNameMessage message)
    {
        if (message.Name.Length > _maxNameLength || message.Name.Length <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-failure"), entity, message.Actor, PopupType.SmallCaution);
            return;
        }

        var nameUpdatedEvent = new VoiceMaskNameUpdatedEvent(entity, entity.Comp.VoiceMaskName, message.Name);
        if (entity.Comp.IsInnate)
            RaiseLocalEvent(entity.Owner, ref nameUpdatedEvent);
        else
            RaiseLocalEvent(message.Actor, ref nameUpdatedEvent);

        entity.Comp.VoiceMaskName = message.Name;
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(message.Actor):player} set voice of {ToPrettyString(entity):mask}: {entity.Comp.VoiceMaskName}");

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-success"), entity, message.Actor);

        UpdateUI(entity);
    }

    private void OnToggle(Entity<VoiceMaskComponent> entity, ref VoiceMaskToggleMessage args)
    {
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-toggle"), entity, args.Actor);
        entity.Comp.Active = !entity.Comp.Active;

        var ev = new VoiceMaskToggledEvent(entity.Owner, args.Actor, entity.Comp.Active);
        RaiseLocalEvent(entity.Owner, ev);

        // Update identity because of possible name override
        _identity.QueueIdentityUpdate(args.Actor);

        UpdateUI(entity);
    }

    private void OnAccentToggle(Entity<VoiceMaskComponent> entity, ref VoiceMaskAccentToggleMessage args)
    {
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-accent-toggle"), entity, args.Actor);
        entity.Comp.AccentHide = !entity.Comp.AccentHide;
        UpdateUI(entity);
    }
    #endregion

    #region UI
    private void OnEquip(EntityUid uid, VoiceMaskComponent component, ClothingGotEquippedEvent args)
    {
        if (_lock.IsLocked(uid))
            return;

        component.Active = false;
        _actions.AddAction(args.Wearer, ref component.ActionEntity, component.Action, uid);
    }

    private void OpenUI(VoiceMaskSetNameEvent ev)
    {
        var maskEntity = ev.Action.Comp.Container;

        if (!TryComp<VoiceMaskComponent>(maskEntity, out var voiceMaskComp))
            return;

        if (!_uiSystem.HasUi(maskEntity.Value, VoiceMaskUIKey.Key))
            return;

        _uiSystem.OpenUi(maskEntity.Value, VoiceMaskUIKey.Key, ev.Performer);
        UpdateUI((maskEntity.Value, voiceMaskComp));
    }

    private void UpdateUI(Entity<VoiceMaskComponent> entity)
    {
        if (_uiSystem.HasUi(entity, VoiceMaskUIKey.Key))
            _uiSystem.SetUiState(entity.Owner, VoiceMaskUIKey.Key, new VoiceMaskBuiState(GetCurrentVoiceName(entity), entity.Comp.VoiceMaskSpeechVerb, entity.Comp.Active, entity.Comp.AccentHide, entity.Comp.TitleText));
    }
    #endregion

    #region Helper functions
    private string GetCurrentVoiceName(Entity<VoiceMaskComponent> entity)
    {
        return entity.Comp.VoiceMaskName ?? Loc.GetString("voice-mask-default-name-override");
    }

    private void TransformVoice(Entity<VoiceMaskComponent> entity, TransformSpeakerNameEvent args)
    {
        if (!entity.Comp.Active)
            return;

        args.VoiceName = GetCurrentVoiceName(entity);
        args.SpeechVerb = entity.Comp.VoiceMaskSpeechVerb ?? args.SpeechVerb;
    }
    #endregion
}

