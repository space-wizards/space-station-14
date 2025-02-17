using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.ADT.SwitchableWeapon;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.SwitchableWeapon;

public sealed class SwitchableWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwitchableWeaponComponent, UseInHandEvent>(Toggle);
        SubscribeLocalEvent<SwitchableWeaponComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SwitchableWeaponComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<SwitchableWeaponComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<SwitchableWeaponComponent, ComponentAdd>(OnComponentAdded);
    }

    private void OnComponentAdded(EntityUid uid, SwitchableWeaponComponent component, ComponentAdd args)
    {
        UpdateState(uid, component);
    }

    //Non-stamina damage
    private void OnGetMeleeDamage(EntityUid uid, SwitchableWeaponComponent component, ref GetMeleeDamageEvent args)
    {
        args.Damage = component.IsOpen ? component.DamageOpen : component.DamageFolded;
    }

    private void OnStaminaHitAttempt(EntityUid uid, SwitchableWeaponComponent component, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!component.IsOpen)
            return;

        //args.HitSoundOverride = component.BonkSound;
    }

    private void OnExamined(EntityUid uid, SwitchableWeaponComponent comp, ExaminedEvent args)
    {
        var msg = comp.IsOpen
            ? Loc.GetString("comp-switchable-examined-on")
            : Loc.GetString("comp-switchable-examined-off");
        args.PushMarkup(msg);
    }

    private void UpdateState(EntityUid uid, SwitchableWeaponComponent comp)
    {
        if (TryComp<ItemComponent>(comp.Owner, out var item))
        {
            _item.SetSize(item.Owner, comp.IsOpen ? comp.SizeOpened : comp.SizeClosed, item);
            _item.SetHeldPrefix(comp.Owner, comp.IsOpen ? "on" : "off", true, item);
        }

        if (TryComp<AppearanceComponent>(comp.Owner, out var appearance))
            _appearance.SetData(comp.Owner, ToggleVisuals.Toggled, comp.IsOpen, appearance);

        // Change stamina damage according to state
        if (TryComp<StaminaDamageOnHitComponent>(uid, out var stamComp))
        {
            stamComp.Damage = comp.IsOpen ? comp.StaminaDamageOpen : comp.StaminaDamageFolded;
        }
    }

    private void Toggle(EntityUid uid, SwitchableWeaponComponent comp, UseInHandEvent args)
    {
        comp.IsOpen = !comp.IsOpen;
        var soundToPlay = comp.IsOpen ? comp.OpenSound : comp.CloseSound;
        _audio.PlayPvs(soundToPlay, args.User);
        UpdateState(uid, comp);
    }
}
