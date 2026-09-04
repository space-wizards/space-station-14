using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Numerics;

namespace Content.Shared.Crayon;

public sealed partial class SharedMagicCrayonSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
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

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.SpawnDelay,
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
        var spawnedFood = PredictedSpawnAtPosition(ent.Comp.FakeFood, spawnCoords);

        _charges.TryUseCharge(ent.Owner);

        if (ent.Comp.OnSpawnSound != null)
        {
            var audioParams = (ent.Comp.OnSpawnSound?.Params ?? AudioParams.Default).WithVariation(0.2f);
            _audio.PlayPredicted(ent.Comp.OnSpawnSound, spawnedFood, user, audioParams);
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
        var normalCrayon = PredictedSpawnAtPosition(ent.Comp.NormalCrayon, coords);
        PredictedDel(ent.Owner);

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
