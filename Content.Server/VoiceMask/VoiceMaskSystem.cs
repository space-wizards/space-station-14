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
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    
    private const string UiGeneratedName = "VoiceMaskBoundUserInterface";
    
    // CCVar.
    private int _maxNameLength;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerNameInventory);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerNameImplant);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<SeeIdentityAttemptEvent>>(OnSeeIdentityAttemptEvent);
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerNameEvent>(OnInnateTransformSpeakerName);
        SubscribeLocalEvent<VoiceMaskComponent, SeeIdentityAttemptEvent>(OnInnateSeeIdentityAttemptEvent);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantImplantedEvent>(OnImplantImplantedEvent);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRemovedEvent>(OnImplantRemovedEventEvent);
        SubscribeLocalEvent<VoiceMaskComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeNameMessage>(OnChangeName);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVerbMessage>(OnChangeVerb);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskToggleMessage>(OnToggle);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskAccentToggleMessage>(OnAccentToggle);
        SubscribeLocalEvent<VoiceMaskComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<VoiceMaskSetNameEvent>(OpenUI);
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeechEvent>(OnTransformSpeech, before: [typeof(AccentSystem)]);
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeechEvent>>(OnTransformSpeechInventory, before: [typeof(AccentSystem)]);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<TransformSpeechEvent>>(OnTransformSpeechImplant, before: [typeof(AccentSystem)]);
        SubscribeLocalEvent<VoiceMaskComponent, MapInitEvent>(OnMapInit);
        
        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
}

    private void OnInnateTransformSpeakerName(Entity<VoiceMaskComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (!ent.Comp.IsInnate)
            return;
            
        TransformVoice(ent, args);
    }

    private void OnMapInit(Entity<VoiceMaskComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.IsInnate)
            return;
            
        _actions.AddAction(ent, ent.Comp.Action);
        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _uiSystem.SetUi((ent, userInterfaceComp), VoiceMaskUIKey.Key, new InterfaceData(UiGeneratedName));
        _identity.QueueIdentityUpdate(ent.Owner);

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
        // Innate voice masks can't be used in the inventory (they only affect themselves)
        if (entity.Comp.IsInnate)
            return;
            
        TransformSpeech(entity, args.Args);
    }

    private void OnTransformSpeechImplant(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<TransformSpeechEvent> args)
    {
        // Innate voice masks can't be implanted
        if (entity.Comp.IsInnate)
            return;
            
        TransformSpeech(entity, args.Event);
    }

    private void OnTransformSpeakerNameInventory(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        // Innate voice masks can't be used in the inventory (they only affect themselves)
        if (entity.Comp.IsInnate)
            return;
            
        TransformVoice(entity, args.Args);
    }

    private void OnTransformSpeakerNameImplant(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<TransformSpeakerNameEvent> args)
    {
        // Innate voice masks can't be implanted
        if (entity.Comp.IsInnate)
            return;
            
        TransformVoice(entity, args.Event);
    }

    /// <summary>
    /// Same as <cref="OnTransformSpeakerNameImplant"> but for innate voice masks
    /// </summary>
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

        args.Event.NameOverride = GetCurrentVoiceName(entity);
    }

    private void OnImplantImplantedEvent(Entity<VoiceMaskComponent> entity, ref ImplantImplantedEvent ev)
    {
        // Innate voice masks can't be implanted
        if (entity.Comp.IsInnate)
            return;
            
        _identity.QueueIdentityUpdate(ev.Implanted);
    }

    private void OnImplantRemovedEventEvent(Entity<VoiceMaskComponent> entity, ref ImplantRemovedEvent ev)
    {
        // Innate voice masks can't be implanted
        if (entity.Comp.IsInnate)
            return;
            
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

        // Update identity because of possible name override
        _identity.QueueIdentityUpdate(args.Actor);
    }

    private void OnAccentToggle(Entity<VoiceMaskComponent> entity, ref VoiceMaskAccentToggleMessage args)
    {
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-accent-toggle"), entity, args.Actor);
        entity.Comp.AccentHide = !entity.Comp.AccentHide;
    }
    #endregion

    #region UI
    private void OnEquip(EntityUid uid, VoiceMaskComponent component, ClothingGotEquippedEvent args)
    {
        if (_lock.IsLocked(uid))
            return;

        // Innate voice masks can't be equiped
        if (component.IsInnate)
            return;
            
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
