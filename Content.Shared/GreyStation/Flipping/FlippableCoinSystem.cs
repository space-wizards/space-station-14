using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.GreyStation.Flipping;

/// <summary>
/// Handles coin flipping.
/// </summary>
public sealed class FlippableCoinSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlippableCoinComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<FlippableCoinComponent, ActivateInWorldEvent>(OnActivate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now  = _timing.CurTime;
        var query = EntityQueryEnumerator<FlippingCoinComponent, FlippableCoinComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var flipping, out var comp, out var appearance))
        {
            if (now < flipping.NextFlip)
                continue;

            RemComp<FlippingCoinComponent>(uid);
            _appearance.SetData(uid, FlippableCoinVisuals.Flipping, false, appearance);

            // if under 1s ping, server has sent updated Flipped state by now
            _appearance.SetData(uid, FlippableCoinVisuals.Flipped, comp.Flipped, appearance);

            // PopupClient doesnt like null user
            if (!_timing.IsFirstTimePredicted || _net.IsServer)
                return;

            var popup = Loc.GetString(comp.Flipped ? comp.TailsPopup : comp.HeadsPopup, ("coin", uid));
            _popup.PopupEntity(popup, uid);
        }
    }

    private void OnUse(Entity<FlippableCoinComponent> ent, ref UseInHandEvent args)
    {
        TryFlip(ent, args.User);
    }

    private void OnActivate(Entity<FlippableCoinComponent> ent, ref ActivateInWorldEvent args)
    {
        TryFlip(ent, args.User);
    }

    public void TryFlip(Entity<FlippableCoinComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        if (HasComp<FlippingCoinComponent>(uid))
            return;

        _audio.PlayPredicted(comp.Sound, uid, user);
        var flipping = EnsureComp<FlippingCoinComponent>(uid);
        flipping.NextFlip = _timing.CurTime + comp.FlipDelay;
        Dirty(uid, flipping);

        _appearance.SetData(uid, FlippableCoinVisuals.Flipping, true);

        // rolled down and not at the end so clients with reasonable ping can fully predict it
        if (_net.IsServer)
        {
            comp.Flipped = _random.Prob(0.5f);
            Dirty(uid, comp);

            _transform.AttachToGridOrMap(uid);
            _throwing.TryThrow(uid, _random.NextVector2(), baseThrowSpeed: 1f, playSound: false);
        }
    }
}
