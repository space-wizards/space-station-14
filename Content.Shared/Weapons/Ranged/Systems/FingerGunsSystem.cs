using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class FingerGunsSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FingerGunsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FingerGunsComponent, UseInHandEvent>(OnActivate, before: new[] { typeof(ClothingSystem) });
        SubscribeLocalEvent<FingerGunsGunComponent, GetVerbsEvent<AlternativeVerb>>(OnGunGetVerbs);
    }

    private void OnMapInit(EntityUid uid, FingerGunsComponent component, MapInitEvent args)
    {

        if (!_net.IsServer)
            return;
        if (component.SkipGunSpawn)
            return;
        var gun = Spawn("WeaponFingerGunsGun", Transform(uid).Coordinates);
        _containers.Insert(gun, _containers.EnsureContainer<ContainerSlot>(uid, "finger_gun"));
    }

    private void OnActivate(EntityUid uid, FingerGunsComponent component, UseInHandEvent args)
    {

        args.Handled = true; // prevents using in hand from trying to equip it to hands slot by default

        if (!_net.IsServer)
            return;

        // Get the hidden gun from the container
        var container = _containers.EnsureContainer<ContainerSlot>(uid, "finger_gun");
        if (container.ContainedEntity is not { } gun)
            return;

        // Store which hand the gloves were in
        if (TryComp<FingerGunsGunComponent>(gun, out var gunComp))
            gunComp.OriginalHand = _hands.GetActiveHand(args.User);

        // Remove gun from container
        _containers.Remove(gun, container);

        // delete gloves
        Del(uid);

        // Put gun in same hand after deleting gloves
        if (gunComp?.OriginalHand != null)
            _hands.TryPickup(args.User, gun, gunComp.OriginalHand);
        else if (_hands.TryGetEmptyHand(args.User, out var hand))
            _hands.TryPickup(args.User, gun, hand);

    }

    private void OnGunGetVerbs(EntityUid uid, FingerGunsGunComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("finger-guns-revert"),
            Act = () =>
            {
                if (!_net.IsServer)
                    return;

                // Spawn fresh glove entity without initializing so can remove gun
                var glove = EntityManager.CreateEntityUninitialized("WeaponFingerGuns", Transform(uid).Coordinates);

                // prevents the entity container from having a gun already in it
                var gloveComp = EnsureComp<FingerGunsComponent>(glove);
                gloveComp.SkipGunSpawn = true;

                // NOW the glove spawns
                EntityManager.InitializeAndStartEntity(glove);

                // Get the gun's container from the new glove and insert gun
                var container = _containers.EnsureContainer<ContainerSlot>(glove, "finger_gun");
                _containers.Insert(uid, container);

                // Put glove in same hand
                if (component.OriginalHand != null)
                    _hands.TryPickup(args.User, glove, component.OriginalHand);
                else if (_hands.TryGetEmptyHand(args.User, out var hand))
                    _hands.TryPickup(args.User, glove, hand);
            }
        });
    }
}
