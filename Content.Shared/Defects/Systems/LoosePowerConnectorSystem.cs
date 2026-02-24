using Content.Shared.Defects.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// Randomly deactivates toggleable weapons with <see cref="LoosePowerConnectorComponent"/> on each swing.
/// </summary>
public sealed class LoosePowerConnectorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoosePowerConnectorComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<LoosePowerConnectorComponent> ent, ref MeleeHitEvent args)
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
        _popup.PopupEntity(Loc.GetString("loose-power-connector-triggered"), ent, args.User, PopupType.SmallCaution);
    }
}
