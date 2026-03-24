using Content.Shared.Defects.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Defects.Systems;

// Randomly deactivates toggleable weapons with LoosePowerConnectorDefectComponent on each swing.
// After deactivating, automatically reactivates after a short delay and plays a spark sound.
public sealed class LoosePowerConnectorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoosePowerConnectorDefectComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<LoosePowerConnectorDefectComponent, ItemToggleComponent>();
        while (query.MoveNext(out var uid, out var connector, out var toggle))
        {
            if (connector.ReactivateAt == null || curTime < connector.ReactivateAt)
                continue;

            connector.ReactivateAt = null;

            if (toggle.Activated)
                continue;

            _itemToggle.TryActivate((uid, toggle));
        }
    }

    private void OnMeleeHit(Entity<LoosePowerConnectorDefectComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!TryComp<ItemToggleComponent>(ent.Owner, out var toggle) || !toggle.Activated)
            return;

        if (_net.IsClient)
            return;

        if (!_random.Prob(ent.Comp.PowerFailChance))
            return;

        _itemToggle.TryDeactivate((ent.Owner, toggle), args.User);
        ent.Comp.ReactivateAt = _timing.CurTime + ent.Comp.ReactivateDelay;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/zzzt.ogg"), ent.Owner);
        _popup.PopupEntity(Loc.GetString("loose-power-connector-triggered"), ent, args.User, PopupType.SmallCaution);
    }
}
