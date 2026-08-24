using Content.Server.Popups;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Crayon;

public sealed partial class MagicCrayonSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicCrayonComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MagicCrayonComponent, MagicCrayonDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, MagicCrayonComponent component, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target != null)
            return;

        if (_charges.IsEmpty(uid))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(0.5f),
            new MagicCrayonDoAfterEvent(GetNetCoordinates(args.ClickLocation)), uid, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, MagicCrayonComponent component, ref MagicCrayonDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target != null)
            return;

        if (_charges.IsEmpty(uid))
            return;

        var spawnCoords = GetCoordinates(args.ClickLocation);
        var spawnedFood = Spawn(component.FakeFood, spawnCoords);

        _charges.TryUseCharge(uid);

        if (_charges.IsEmpty(uid))
        {
            UseUp(uid, args.User);
            SpawnNormalCrayon(component, args.User);
        }

        if (component.OnSpawnSound != null)
        {
            var audioParams = (component.OnSpawnSound?.Params ?? AudioParams.Default).WithVariation(0.2f);
            _audio.PlayPvs(component.OnSpawnSound, spawnedFood, audioParams);
        }

        args.Handled = true;
    }

    private void UseUp(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);
        Del(uid);
    }

    private void SpawnNormalCrayon(MagicCrayonComponent comp, EntityUid user)
    {
        if (_hands.TryGetEmptyHand(user, out var hand))
        {
            var mimeCrayon = Spawn(comp.NormalCrayon);
            _inventory.TryEquip(user, mimeCrayon, hand);
        }
        else
        {
            var coords = Transform(user).Coordinates.Offset(new(0.5f, 0.0f));
            Spawn(comp.NormalCrayon, coords);
        }
    }
}
