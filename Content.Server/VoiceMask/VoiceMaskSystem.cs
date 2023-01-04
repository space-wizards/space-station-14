using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Content.Shared.VoiceMask;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeNameMessage>(OnChangeName);
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
            _popupSystem.PopupCursor(Loc.GetString("voice-mask-popup-failure"), message.Session);
            return;
        }

        component.VoiceName = message.Name;

        _popupSystem.PopupCursor(Loc.GetString("voice-mask-popup-success"), message.Session);

        TrySetLastKnownName(uid, message.Name);

        UpdateUI(uid, component);
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
        }
    }

    private void OpenUI(EntityUid player, ActorComponent? actor = null)
    {
        if (!Resolve(player, ref actor))
        {
            return;
        }

        _uiSystem.GetUiOrNull(player, VoiceMaskUIKey.Key)?.Open(actor.PlayerSession);
        UpdateUI(player);
    }

    private void UpdateUI(EntityUid owner, VoiceMaskComponent? component = null)
    {
        if (!Resolve(owner, ref component))
        {
            return;
        }

        _uiSystem.GetUiOrNull(owner, VoiceMaskUIKey.Key)?.SetState(new VoiceMaskBuiState(component.VoiceName));
    }
}

public sealed class VoiceMaskSetNameEvent : InstantActionEvent
{
}
