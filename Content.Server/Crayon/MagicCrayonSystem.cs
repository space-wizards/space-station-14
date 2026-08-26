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

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<MagicCrayonComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target != null)
            return;

        if (!args.CanReach)
        {
            _popup.PopupCursor(Loc.GetString("crayon-interact-invalid-location"), args.User);
            return;
        }

        if (_charges.IsEmpty(ent.Owner))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(0.5f),
            new MagicCrayonDoAfterEvent(GetNetCoordinates(args.ClickLocation)), ent, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<MagicCrayonComponent> ent, ref MagicCrayonDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target != null)
            return;

        var user = args.User;
        var spawnCoords = GetCoordinates(args.ClickLocation);
        var spawnedFood = Spawn(ent.Comp.FakeFood, spawnCoords);

        _charges.TryUseCharge(ent.Owner);

        if (ent.Comp.OnSpawnSound != null)
        {
            var audioParams = (ent.Comp.OnSpawnSound?.Params ?? AudioParams.Default).WithVariation(0.2f);
            _audio.PlayPvs(ent.Comp.OnSpawnSound, spawnedFood, audioParams);
        }

        if (_charges.IsEmpty(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", ent)), user, user);
            MutateToNormal(ent, user);
            args.Handled = true;
            return;
        }

        _adminLog.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):user} drew a {ToPrettyString(spawnedFood)} with {ToPrettyString(ent):used}");
        args.Handled = true;
    }

    /// <summary>
    /// Deletes the magic crayon and spawns a normal one as defined in <see cref="MagicCrayonComponent.NormalCrayon"/>.
    /// </summary>
    /// <param name="ent">The magic crayon to mutate.</param>
    /// <param name="user">The entity who mutated the magic crayon.</param>
    private void MutateToNormal(Entity<MagicCrayonComponent> ent, EntityUid? user)
    {
        var coords = Transform(ent).Coordinates;
        var normalCrayon = Spawn(ent.Comp.NormalCrayon);
        Del(ent);

        if (!user.HasValue)
        {
            _transform.SetCoordinates(normalCrayon, coords);
            return;
        }

        if (!_hands.TryPickupAnyHand(user.Value, normalCrayon))
        {
            _transform.SetCoordinates(normalCrayon, coords.Offset(new Vector2(0.5f, 0.0f)));
        }
    }
}
