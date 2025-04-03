// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Emag.Components;
using Robust.Shared.Utility;
using Content.Shared.Hands;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Ghost;
using Content.Shared.Item;

namespace Content.Shared.DeadSpace.Dominator;

public sealed class DominatorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DominatorComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<DominatorComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<DominatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DominatorComponent, GetVerbsEvent<AlternativeVerb>>(GetAlternativeVerb);
        SubscribeLocalEvent<DominatorComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<DominatorComponent, GotEquippedHandEvent>(OnHandEquip);
        SubscribeLocalEvent<DominatorComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnExamined(EntityUid uid, DominatorComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        var fireMode = GetMode(component);

        if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", proto.Name)));

        if (args.IsInDetailsRange && HasComp<EmaggedComponent>(uid))
            args.PushMarkup(Loc.GetString("dominator-compromised-examine"));
    }

    private BatteryWeaponFireMode GetMode(DominatorComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }

    private void OnGetVerb(EntityUid uid, DominatorComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.FireModes.Count < 2)
            return;

        for (var i = 0; i < component.FireModes.Count; i++)
        {
            var fireMode = component.FireModes[i];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = i == component.CurrentFireMode,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(EntityUid uid, DominatorComponent component, ActivateInWorldEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelay)
            && _useDelay.IsDelayed((uid, useDelay)))
            return;

        CycleFireMode(uid, component, args.User);
    }

    private void CycleFireMode(EntityUid uid, DominatorComponent component, EntityUid user)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        SetFireMode(uid, component, index, user);
    }

    private void SetFireMode(EntityUid uid, DominatorComponent component, int index, EntityUid? user = null)
    {
        if (TryComp<UseDelayComponent>(uid, out var useDelay)
            && _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!HasComp<EmaggedComponent>(uid))
        {
            if (user != null)
            {
                if (component.OwnerIdCard == null || component.OwnerIdCard != component.LastHoldingIdCard)
                {
                    _popupSystem.PopupClient(Loc.GetString("dominator-permission-denied"), uid, user.Value);
                    return;
                }
            }
        }

        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
                return;

            projectileBatteryAmmoProvider.Prototype = fireMode.Prototype;
            projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;

            if (user != null)
            {
                _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, user.Value);
                _audio.PlayPredicted(fireMode.SwitchSound, uid, user, AudioParams.Default.WithVolume(-4f));
            }
        }

        if (TryComp(uid, out AppearanceComponent? appearance))
        {
            _appearance.QueueUpdate(uid, appearance); // for icon update
            _item.VisualsChanged(uid); // for inhand update
        }

        if (useDelay != null)
            _useDelay.TryResetDelay((uid, useDelay));
    }

    public void GetAlternativeVerb(EntityUid uid, DominatorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!args.CanInteract && !HasComp<GhostComponent>(args.User))
            return;

        if (component.OwnerIdCard == null && !HasComp<EmaggedComponent>(uid))
        {
            var setOwnerIdCardVerb = new AlternativeVerb
            {
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/character.svg.192dpi.png")),
                Text = Loc.GetString("dominator-verb-set-owner"),
                Priority = 1,
                Act = () =>
                {
                    if (!args.CanAccess)
                        return;

                    if (!args.CanInteract && !HasComp<GhostComponent>(args.User))
                        return;

                    if (!_idCard.TryFindIdCard(args.User, out var idCard))
                    {
                        _popupSystem.PopupClient(Loc.GetString("dominator-id-card-not-found"), uid, args.User);
                    }
                    else
                    {
                        if (TryComp<UseDelayComponent>(uid, out var useDelay) && !_useDelay.IsDelayed((uid, useDelay)))
                        {
                            component.OwnerIdCard = idCard.Owner;
                            _audio.PlayPredicted(component.SetOwnerSound, uid, args.User, AudioParams.Default.WithVolume(-4f));
                            if (useDelay != null)
                                _useDelay.TryResetDelay((uid, useDelay));
                        }
                    }
                }
            };
            args.Verbs.Add(setOwnerIdCardVerb);
        }
        if (component.OwnerIdCard != null && !HasComp<EmaggedComponent>(uid))
        {
            var clearOwnerIdCardVerb = new AlternativeVerb
            {
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Text = Loc.GetString("dominator-verb-clear-owner"),
                Priority = 1,
                Act = () =>
                {
                    if (!args.CanAccess)
                        return;

                    if (!args.CanInteract && !HasComp<GhostComponent>(args.User))
                        return;

                    if (!_access.IsAllowed(args.User, uid))
                    {
                        _popupSystem.PopupClient(Loc.GetString("dominator-permission-denied"), uid, args.User);
                    }
                    else
                    {
                        if (TryComp<UseDelayComponent>(uid, out var useDelay) && !_useDelay.IsDelayed((uid, useDelay)))
                        {
                            component.OwnerIdCard = null;
                            _audio.PlayPredicted(component.ClearOwnerSound, uid, args.User, AudioParams.Default.WithVolume(-4f));
                            if (useDelay != null)
                                _useDelay.TryResetDelay((uid, useDelay));
                        }
                    }
                }
            };
            args.Verbs.Add(clearOwnerIdCardVerb);
        }
    }

    private void OnShotAttempted(EntityUid uid, DominatorComponent component, ref ShotAttemptedEvent args)
    {
        if (HasComp<EmaggedComponent>(uid))
            return;

        if (component.OwnerIdCard == null || component.OwnerIdCard != component.LastHoldingIdCard)
        {
            args.Cancel();
            _popupSystem.PopupClient(Loc.GetString("dominator-permission-denied"), uid, args.User);
            return;
        }

        if (TryComp<UseDelayComponent>(uid, out var useDelay) && _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider) && projectileBatteryAmmoProvider.Shots == 0)
        {
            _audio.PlayPredicted(component.LowBatterySound, uid, args.User, AudioParams.Default.WithVolume(-4f));
            if (useDelay != null)
                _useDelay.TryResetDelay((uid, useDelay));
        }
    }

    /// <summary>
    ///     Because of prediction we cant check for id-card in OnShotAttempted, so we check every time entity thats grabs dominator. FIX AND NUKE THIS PLEASE.
    /// </summary>
    private void OnHandEquip(EntityUid uid, DominatorComponent component, GotEquippedHandEvent args)
    {
        if (_idCard.TryFindIdCard(args.User, out var idCard))
        {
            component.LastHoldingIdCard = idCard;
        }
        else
        {
            component.LastHoldingIdCard = null;
        }
    }

    private void OnEmagged(EntityUid uid, DominatorComponent component, ref GotEmaggedEvent args)
    {
        _audio.PlayPredicted(component.EmagSound, uid, args.UserUid);
        args.Handled = true;
    }
}
