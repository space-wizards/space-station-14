using Content.Server.Chat.Systems;
using Content.Server.Popups;
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
        SubscribeLocalEvent<VoiceMaskerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<VoiceMaskerComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<VoiceMaskerComponent, VoiceMaskChangeNameMessage>(OnChangeName);
        SubscribeLocalEvent<VoiceMaskerComponent, GetVerbsEvent<AlternativeVerb>>(GetVerbs);
    }

    private void OnChangeName(EntityUid uid, VoiceMaskerComponent component, VoiceMaskChangeNameMessage msg)
    {
        if (msg.Name.Length > HumanoidCharacterProfile.MaxNameLength || msg.Name.Length <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("voice-mask-popup-failure"), Filter.SinglePlayer(msg.Session));
            return;
        }

        // probably not the best identifier
        var owner = Transform(uid).ParentUid;
        if (!TryComp(owner, out VoiceMaskComponent? mask))
        {
            return;
        }

        component.LastSetName = msg.Name;
        mask.VoiceName = msg.Name;

        _popupSystem.PopupCursor(Loc.GetString("voice-mask-popup-success"), Filter.SinglePlayer(msg.Session));

        UpdateUI(uid, owner, mask);
    }

    private void GetVerbs(EntityUid uid, VoiceMaskerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<VoiceMaskComponent>(args.User) || !args.CanInteract)
        {
            return;
        }

        var verb = new AlternativeVerb();
        verb.Text = Loc.GetString("voice-mask-name-change-set");
        verb.Act = () => OpenUI(uid, args.User);
        args.Verbs.Add(verb);
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

    private void OpenUI(EntityUid mask, EntityUid player, ActorComponent? actor = null)
    {
        if (!Resolve(player, ref actor))
        {
            return;
        }

        UpdateUI(mask, player);
        _uiSystem.GetUiOrNull(mask, VoiceMaskUIKey.Key)?.Open(actor.PlayerSession);
    }

    private void UpdateUI(EntityUid mask, EntityUid owner, VoiceMaskComponent? component = null)
    {
        if (!Resolve(owner, ref component))
        {
            return;
        }

        _uiSystem.GetUiOrNull(mask, VoiceMaskUIKey.Key)?.SetState(new VoiceMaskBuiState(component.VoiceName));
    }
}
