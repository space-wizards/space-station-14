using Content.Server._Impstation.Chaplain.Components;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using System.Linq;
using Content.Shared._Impstation.Chaplain;
using Content.Server.Bible.Components;
using Robust.Shared.Audio.Systems;
using Content.Server.Popups;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Content.Shared.Damage.Prototypes;
using Content.Server.Stunnable;

namespace Content.Server._Impstation.Chaplain.Systems;

/// <summary>
/// <see cref="ChaplainGearMenuComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed class ChaplainGearMenuSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioshared = default!;
    [Dependency] private readonly StunSystem _stun = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainGearMenuComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<ChaplainGearMenuComponent, ChaplainGearMenuApproveMessage>(OnApprove);
        SubscribeLocalEvent<ChaplainGearMenuComponent, ChaplainGearChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<ChaplainGearMenuComponent> backpack, ref BoundUIOpenedEvent args)
    {
        var dmgProt = _proto.Index((ProtoId<DamageTypePrototype>)"Caustic");
        var searDamage = new DamageSpecifier(dmgProt, 15f);
        var soundSizzle = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        if (!HasComp<BibleUserComponent>(args.Actor))
        {
            _popupSystem.PopupEntity(Loc.GetString("null-rod-rejection"), args.Actor, args.Actor);

            _audioshared.PlayPvs(soundSizzle, args.Actor);
            _damageableSystem.TryChangeDamage(args.Actor, searDamage, true);

            _stun.TryKnockdown(args.Actor, TimeSpan.FromSeconds(1.5f), true);
            _stun.TryStun(args.Actor, TimeSpan.FromSeconds(1.5f), true);
            _ui.CloseUi(args.Entity, ChaplainGearMenuUIKey.Key);
            return;
        }
        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void OnApprove(Entity<ChaplainGearMenuComponent> backpack, ref ChaplainGearMenuApproveMessage args)
    {
        var soundApprove = new SoundPathSpecifier("/Audio/Effects/holy.ogg");
        if (backpack.Comp.SelectedSets.Count != backpack.Comp.MaxSelectedSets)
            return;

        foreach (var i in backpack.Comp.SelectedSets)
        {
            var set = _proto.Index(backpack.Comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                var ent = Spawn(item, _transform.GetMapCoordinates(backpack.Owner));
                if (HasComp<ItemComponent>(ent))
                    _hands.TryPickupAnyHand(args.Actor, ent);
            }
        }
        _audioshared.PlayPvs(soundApprove, args.Actor);
        _popupSystem.PopupEntity(Loc.GetString("null-rod-transformed"), args.Actor, args.Actor);
        QueueDel(backpack);
    }
    private void OnChangeSet(Entity<ChaplainGearMenuComponent> backpack, ref ChaplainGearChangeSetMessage args)
    {
        //Swith selecting set
        if (!backpack.Comp.SelectedSets.Remove(args.SetNumber))
            backpack.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void UpdateUI(EntityUid uid, ChaplainGearMenuComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, ChaplainGearMenuSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new ChaplainGearMenuSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, ChaplainGearMenuUIKey.Key, new ChaplainGearMenuBoundUserInterfaceState(data, component.MaxSelectedSets));
    }
}
