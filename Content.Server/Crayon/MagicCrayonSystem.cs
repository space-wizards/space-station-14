using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Numerics;

namespace Content.Server.Crayon;

public sealed partial class MagicCrayonSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicCrayonComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MagicCrayonComponent, MagicCrayonDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, MagicCrayonComponent component, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target != null)
            return;

        if (!args.CanReach)
        {
            _popup.PopupCursor(Loc.GetString("crayon-interact-invalid-location"), args.User);
            return;
        }

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

        var user = args.User;
        var spawnCoords = GetCoordinates(args.ClickLocation);
        var spawnedFood = Spawn(component.FakeFood, spawnCoords);

        _charges.TryUseCharge(uid);

        if (component.OnSpawnSound != null)
        {
            var audioParams = (component.OnSpawnSound?.Params ?? AudioParams.Default).WithVariation(0.2f);
            _audio.PlayPvs(component.OnSpawnSound, spawnedFood, audioParams);
        }

        if (_charges.IsEmpty(uid))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);
            MutateToNormal(user, uid, component);
        }

        _adminLog.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):user} drew a {ToPrettyString(spawnedFood):fakeFood} with {ToPrettyString(uid)}");
        args.Handled = true;
    }

    private void MutateToNormal(EntityUid user, EntityUid used, MagicCrayonComponent comp)
    {
        Del(used);
        var mimeCrayon = Spawn(comp.NormalCrayon, Transform(user).Coordinates);

        if (!_hands.TryPickupAnyHand(user, mimeCrayon))
        {
            var coords = Transform(user).Coordinates.Offset(new Vector2(0.5f, 0.0f));
            _transform.SetCoordinates(mimeCrayon, coords);
        }
    }
}
