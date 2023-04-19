using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// System for katana dashing and binding
/// </summary>
public sealed class EnergyKatanaSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyKatanaComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, KatanaDashEvent>(OnDash);
    }

    private void OnEquipped(EntityUid uid, EnergyKatanaComponent comp, GotEquippedEvent args)
    {
        // check if already bound
        if (comp.Ninja != null)
            return;

        // check if ninja already has a katana bound
        var user = args.Equipee;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.Katana != null)
            return;

        // bind it
        comp.Ninja = user;
        _ninja.BindKatana(ninja, uid);
    }

    public void OnDash(EntityUid suit, NinjaSuitComponent comp, KatanaDashEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var user = args.Performer;
        args.Handled = true;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.Katana == null)
            return;

        var uid = ninja.Katana.Value;
        if (!TryComp<EnergyKatanaComponent>(uid, out var katana) || !_hands.IsHolding(user, uid, out var _))
        {
            ClientPopup("ninja-katana-not-held", user);
            return;
        }

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            ClientPopup("ninja-katana-no-charges", user);
            return;
        }

        var origin = Transform(user).MapPosition;
        var target = args.Target.ToMap(EntityManager, _transform);
        // prevent collision with the user duh
        if (!_interaction.InRangeUnobstructed(origin, target, 0f, CollisionGroup.Opaque, uid => uid == user))
        {
            // can only dash if the destination is visible on screen
            ClientPopup("ninja-katana-cant-see", user);
            return;
        }

        _transform.SetCoordinates(user, args.Target);
        _transform.AttachToGridOrMap(user);
        _audio.PlayPredicted(katana.BlinkSound, user, user, AudioParams.Default.WithVolume(katana.BlinkVolume));
        if (charges != null)
            _charges.UseCharge(uid, charges);
    }

    private void ClientPopup(string msg, EntityUid user)
    {
        if (_net.IsClient)
            _popups.PopupEntity(Loc.GetString(msg), user);
    }
}
