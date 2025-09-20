using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCProximitySleepSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCProximitySleepComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCProximitySleepComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var actorLocations = GetActorLocations();

        var query = EntityQueryEnumerator<NPCProximitySleepComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var proxComp, out _))
        {
            if (proxComp.NextUpdate >= _timing.CurTime)
                continue;

            var npcPosition = _transform.GetMapCoordinates(uid);

            var pause = true;
            if (actorLocations.TryGetValue(npcPosition.MapId, out var actors))
            {
                foreach (var cordActor in actors)
                {
                    if (_whitelist.CheckBoth(cordActor.Actor, proxComp.ProximityDontIgnore, proxComp.ProximityIgnore))
                        continue;

                    var distance = (cordActor.Location - npcPosition.Position).Length();

                    if (distance >= proxComp.UnpauseProximity)
                        continue;

                    pause = false;
                    break;
                }
            }

            if (pause)
                _npc.SleepNPC(uid, NPCSleepingCategories.ProxySleep);
            else
                _npc.WakeNPC(uid, NPCSleepingCategories.ProxySleep);

            proxComp.NextUpdate += proxComp.UpdateInterval;

        }
    }

    // Get a dictionary of maps -> actor cords in that map.
    private Dictionary<MapId, List<(Vector2 Location, EntityUid Actor)>> GetActorLocations()
    {
        var actorLocations = new Dictionary<MapId, List<(Vector2, EntityUid)>>();

        var actorEnumerator = EntityQueryEnumerator<ActorComponent>();
        while (actorEnumerator.MoveNext(out var actorUid, out _))
        {
            var actorPos = _transform.GetMapCoordinates(actorUid);

            // Add the actors location
            var pair = (actorPos.Position, actorUid);
            if (!actorLocations.TryGetValue(actorPos.MapId, out var cordList))
                actorLocations.Add(actorPos.MapId, [pair]);
            else
                cordList.Add(pair);
        }

        return actorLocations;
    }
}
