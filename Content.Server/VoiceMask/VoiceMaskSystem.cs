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
using Content.Shared.Preferences;
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

    // CCVar.
    private int _maxNameLength;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerNameInventory);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerNameImplant);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRelayEvent<SeeIdentityAttemptEvent>>(OnSeeIdentityAttemptEvent);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantImplantedEvent>(OnImplantImplantedEvent);
        SubscribeLocalEvent<VoiceMaskComponent, ImplantRemovedEvent>(OnImplantRemovedEventEvent);
        SubscribeLocalEvent<VoiceMaskComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeNameMessage>(OnChangeName);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVerbMessage>(OnChangeVerb);
        SubscribeLocalEvent<VoiceMaskComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<VoiceMaskSetNameEvent>(OpenUI);

        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    private void OnTransformSpeakerNameInventory(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        TransformVoice(entity, args.Args);
    }

    private void OnTransformSpeakerNameImplant(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<TransformSpeakerNameEvent> args)
    {
        TransformVoice(entity, args.Event);
    }

    private void OnSeeIdentityAttemptEvent(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<SeeIdentityAttemptEvent> args)
    {
        if (entity.Comp.OverrideIdentity)
            args.Event.NameOverride = GetCurrentVoiceName(entity);
    }

    private void OnImplantImplantedEvent(Entity<VoiceMaskComponent> entity, ref ImplantImplantedEvent ev)
    {
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
        RaiseLocalEvent(message.Actor, ref nameUpdatedEvent);

        entity.Comp.VoiceMaskName = message.Name;
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(message.Actor):player} set voice of {ToPrettyString(entity):mask}: {entity.Comp.VoiceMaskName}");

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-success"), entity, message.Actor);

        UpdateUI(entity);
    }
    #endregion

    #region UI
    private void OnEquip(EntityUid uid, VoiceMaskComponent component, ClothingGotEquippedEvent args)
    {
        if (_lock.IsLocked(uid))
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
            _uiSystem.SetUiState(entity.Owner, VoiceMaskUIKey.Key, new VoiceMaskBuiState(GetCurrentVoiceName(entity), entity.Comp.VoiceMaskSpeechVerb));
    }
    #endregion

    #region Helper functions
    private string GetCurrentVoiceName(Entity<VoiceMaskComponent> entity)
    {
        return entity.Comp.VoiceMaskName ?? Loc.GetString("voice-mask-default-name-override");
    }

    private void TransformVoice(Entity<VoiceMaskComponent> entity, TransformSpeakerNameEvent args)
    {
        args.VoiceName = GetCurrentVoiceName(entity);
        args.SpeechVerb = entity.Comp.VoiceMaskSpeechVerb ?? args.SpeechVerb;
    }
    #endregion
}
