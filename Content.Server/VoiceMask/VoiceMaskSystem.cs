using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Speech;
using Content.Shared.VoiceMask;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeNameMessage>(OnChangeName);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVerbMessage>(OnChangeVerb);
        SubscribeLocalEvent<VoiceMaskComponent, WearerMaskToggledEvent>(OnMaskToggled);
        SubscribeLocalEvent<VoiceMaskerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<VoiceMaskerComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<VoiceMaskSetNameEvent>(OnSetName);
        // SubscribeLocalEvent<VoiceMaskerComponent, GetVerbsEvent<AlternativeVerb>>(GetVerbs);
    }

    private void OnSetName(VoiceMaskSetNameEvent ev)
    {
        OpenUI(ev.Performer);
    }

    private void OnChangeName(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeNameMessage message)
    {
        if (message.Name.Length > HumanoidCharacterProfile.MaxNameLength || message.Name.Length <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-failure"), uid, message.Session, PopupType.SmallCaution);
            return;
        }

        component.VoiceName = message.Name;
        if (message.Session.AttachedEntity != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(message.Session.AttachedEntity.Value):player} set voice of {ToPrettyString(uid):mask}: {component.VoiceName}");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"Voice of {ToPrettyString(uid):mask} set: {component.VoiceName}");

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-success"), uid, message.Session);

        TrySetLastKnownName(uid, message.Name);

        UpdateUI(uid, component);
    }

    private void OnChangeVerb(Entity<VoiceMaskComponent> ent, ref VoiceMaskChangeVerbMessage msg)
    {
        if (msg.Verb is {} id && !_proto.HasIndex<SpeechVerbPrototype>(id))
            return;

        ent.Comp.SpeechVerb = msg.Verb;
        // verb is only important to metagamers so no need to log as opposed to name

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-success"), ent, msg.Session);

        TrySetLastSpeechVerb(ent, msg.Verb);

        UpdateUI(ent, ent.Comp);
    }

    private void OnSpeakerNameTransform(EntityUid uid, VoiceMaskComponent component, TransformSpeakerNameEvent args)
    {
        if (component.Enabled)
        {
            /*
            args.Name = _idCard.TryGetIdCard(uid, out var card) && !string.IsNullOrEmpty(card.FullName)
                ? card.FullName
                : Loc.GetString("voice-mask-unknown");
                */

            args.Name = component.VoiceName;
            if (component.SpeechVerb != null)
                args.SpeechVerb = component.SpeechVerb;
        }
    }

    private void OnMaskToggled(Entity<VoiceMaskComponent> ent, ref WearerMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.IsToggled;
    }

    private void OpenUI(EntityUid player, ActorComponent? actor = null)
    {
        if (!Resolve(player, ref actor))
            return;
        if (!_uiSystem.TryGetUi(player, VoiceMaskUIKey.Key, out var bui))
            return;

        _uiSystem.OpenUi(bui, actor.PlayerSession);
        UpdateUI(player);
    }

    private void UpdateUI(EntityUid owner, VoiceMaskComponent? component = null)
    {
        if (!Resolve(owner, ref component))
        {
            return;
        }

        if (_uiSystem.TryGetUi(owner, VoiceMaskUIKey.Key, out var bui))
            _uiSystem.SetUiState(bui, new VoiceMaskBuiState(component.VoiceName, component.SpeechVerb));
    }
}
