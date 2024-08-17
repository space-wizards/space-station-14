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
using Content.Server.Popups;
using Content.Shared.Popups;

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
    [Dependency] private readonly PopupSystem _popups = default!;

    private const string TeleportEffectPrototype = "TeleportEffect";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SyndicateTeleporterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.InWall == true)
            {
                comp.Timer += frameTime;
                if (comp.Timer >= comp.CorrectTime) //I'm not sure I should do it this way...
                {
                    SaveTeleport(uid, comp);
                    comp.Timer = 0;
                }
            }
        }
    }

    private void OnUse(EntityUid uid, SyndicateTeleporterComponent component, UseInHandEvent args)
    {
        component.UserComp = args.User; // well, i need this for SaveTeleport...

        if (!TryComp<LimitedChargesComponent>(uid, out var charges))
            return;

        if (args.Handled)
            return;

        if (_charges.IsEmpty(uid, charges))
            return;

        _charges.UseCharge(uid, charges);

        Teleportation(uid, args.User, component);
    }

    private void Teleportation(EntityUid uid, EntityUid user, SyndicateTeleporterComponent comp)
    {
        float random = _random.Next(0, comp.RandomDistanceValue);
        var multiplaer = new Vector2(comp.TeleportationValue + random, comp.TeleportationValue + random); //make random for teleport distance valu

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

        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            comp.InWall = true; // if yes then starting the timer countdown in update

        }

    }

    private void SaveTeleport(EntityUid uid, SyndicateTeleporterComponent comp)
    {
        var transform = Transform(comp.UserComp);
        var offsetValue = Transform(comp.UserComp).LocalPosition;
        var coords = transform.Coordinates.WithPosition(offsetValue);

        var tile = coords.GetTileRef(EntityManager, _mapMan);
        if (tile == null)
            return;

        var saveattempts = comp.SaveAttempts;
        var savedistance = comp.SaveDistance;

        while (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            if (!TryComp<BodyComponent>(comp.UserComp, out var body))
                return;


            EntityUid? tuser = null;

            if (saveattempts > 0) // if we have chance to survive then teleport in random side away
            {
                double side = _random.Next(-180, 180);
                offsetValue = Angle.FromDegrees(side).ToWorldVec() * savedistance; //averages the resulting direction, turning it into one of 8 directions, (N, NE, E...)
                coords = transform.Coordinates.Offset(offsetValue);
                _transformSystem.SetCoordinates(comp.UserComp, coords);

                Spawn(TeleportEffectPrototype, coords);
                _audio.PlayPredicted(comp.AlarmSound, uid, tuser);

                saveattempts--;
            }
            else
            {
                _body.GibBody(comp.UserComp, true, body);
                comp.InWall = false; // closing the countdown in update
                break;
            }

            tile = coords.GetTileRef(EntityManager, _mapMan);
            if(tile == null)
            {
                return;
            }
            if (!_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            {
                comp.InWall = false;
                return;
            }
        }
    }
}
