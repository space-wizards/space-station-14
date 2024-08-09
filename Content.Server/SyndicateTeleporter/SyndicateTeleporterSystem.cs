using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Audio.Systems;
using System.Threading.Tasks;
using Robust.Shared.Random;

namespace Content.Server.SyndicateTeleporter;

public sealed class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    [ValidatePrototypeId<EntityPrototype>]
    private const string TeleportEffectPrototype = "TeleportEffect";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(EntityUid uid, SyndicateTeleporterComponent component, UseInHandEvent args)
    {
        if (!TryComp<LimitedChargesComponent>(uid, out var charges))
            return;

        if (args.Handled)
            return;

        if (_charges.IsEmpty(uid, charges))
            return;

        _charges.UseCharge(uid, charges);

        Teleportation(uid, args.User, component);
    }

    private async Task Teleportation(EntityUid uid, EntityUid user, SyndicateTeleporterComponent comp)
    {
        float random = _random.Next(0, comp.RandomDistanceValue);
        var multiplaer = new Vector2(comp.TeleportationValue + random, comp.TeleportationValue + random); //make random for teleport distance value

        EntityUid? tuser = null;

        var transform = Transform(user);
        var offsetValue = transform.LocalRotation.ToWorldVec().Normalized() * multiplaer;
        var coords = transform.Coordinates.Offset(offsetValue); //set coordinates where we move on

        Spawn(TeleportEffectPrototype, Transform(user).Coordinates); 

        if (transform.MapID != coords.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(user, coords); // teleport

        Spawn(TeleportEffectPrototype, Transform(user).Coordinates);

        var tile = coords.GetTileRef(EntityManager, _mapMan); // get info about place where we just teleported. theare a walls?
        if (tile == null)
            return;

        var saveattempts = comp.SaveAttempts;
        var savedistance = comp.SaveDistance;

        while (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            if (!TryComp<BodyComponent>(user, out var body)) 
                return;


            if (saveattempts > 0) // if we have chance to survive then teleport in random side away
            {
                await Task.Delay(400);
                double side = _random.Next(-180, 180);
                offsetValue = Angle.FromDegrees(side).ToWorldVec() * savedistance; //averages the resulting direction, turning it into one of 8 directions, (N, NE, E...)
                coords = transform.Coordinates.Offset(offsetValue);
                _transformSystem.SetCoordinates(user, coords);

                Spawn(TeleportEffectPrototype, coords);
                _audio.PlayPredicted(comp.AlarmSound, uid, tuser);

                saveattempts--;
            }
            else
            {
                _body.GibBody(user, true, body);
                break;
            }


            tile = coords.GetTileRef(EntityManager, _mapMan);
            if (tile == null)
                return;

        }
    }
}
