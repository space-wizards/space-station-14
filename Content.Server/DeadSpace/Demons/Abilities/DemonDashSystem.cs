// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Interaction;
using Content.Shared.DeadSpace.Demons.Abilities.Components;
using Content.Shared.DeadSpace.Demons.Abilities;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Demons.Abilities;

public sealed class DemonDashSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonDashComponent, DemonDashEvent>(OnDash);
        SubscribeLocalEvent<DemonDashComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DemonDashComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DemonDashComponent, LimitedChargesComponent>();
        while (query.MoveNext(out var uid, out var comp, out var charges))
        {
            if (_timing.CurTime > comp.AddChargeTime)
            {
                _charges.AddCharges(uid, 1, charges);
                comp.AddChargeTime = _timing.CurTime + comp.AddChargeDuration;
                _popup.PopupEntity(Loc.GetString($"Рывок восстановился, количество рывков = {charges.Charges}"), uid, uid);
            }
        }
    }

    private void OnComponentInit(EntityUid uid, DemonDashComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.DemonDashActionEntity, component.DemonDashAction, uid);
    }

    private void OnComponentShutdown(EntityUid uid, DemonDashComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.DemonDashActionEntity);
    }

    private void OnDash(EntityUid uid, DemonDashComponent comp, DemonDashEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        var user = args.Performer;
        args.Handled = true;

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете применить телепортацию, количество рывков = 0"), uid, uid);
            return;
        }

        var origin = Transform(user).MapPosition;
        var target = args.Target.ToMap(EntityManager, _transform);
        if (!_interaction.InRangeUnobstructed(origin, target, 0f, CollisionGroup.Opaque, uid => uid == user))
        {
            _popup.PopupEntity(Loc.GetString("Конечная точка вне зоны видимости"), uid, uid);
            return;
        }
        _transform.SetCoordinates(user, args.Target);
        _transform.AttachToGridOrMap(user);

        _audio.PlayPvs("/Audio/Magic/blink.ogg", uid, AudioParams.Default.WithVolume(3).WithMaxDistance(2f));

        if (charges != null)
        {
            _charges.UseCharge(uid, charges);
            _popup.PopupEntity(Loc.GetString($"Количество рывков = {charges.Charges}"), uid, uid);
        }

        _audio.PlayPvs("/Audio/Magic/blink.ogg", uid, AudioParams.Default.WithVolume(3).WithMaxDistance(2f));
    }
}
