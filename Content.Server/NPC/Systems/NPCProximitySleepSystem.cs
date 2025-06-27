using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var actorLocations = GetActorLocations();

        var query = EntityQueryEnumerator<NPCProximitySleepComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var proxComp, out _))
        {
            if (proxComp.LastUpdate >= _timing.CurTime)
                continue;

            var npcPosition = _transform.GetMapCoordinates(uid);

            var pause = true;
            if (actorLocations.TryGetValue(npcPosition.MapId, out var cordList))
            {
                foreach (var cord in cordList)
                {
                    var distance = (cord - npcPosition.Position).Length();

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

            proxComp.LastUpdate += proxComp.UpdateInterval;

        }
    }

    // Get a dictionary of maps -> actor cords in that map.
    private Dictionary<MapId, List<Vector2>> GetActorLocations()
    {
        var actorLocations = new Dictionary<MapId, List<Vector2>>();

        var actorEnumerator = EntityQueryEnumerator<ActorComponent>();
        while (actorEnumerator.MoveNext(out var actorUid, out _))
        {
            var actorPos = _transform.GetMapCoordinates(actorUid);

            // Add the actors location
            if (!actorLocations.TryGetValue(actorPos.MapId, out var cordList))
                actorLocations.Add(actorPos.MapId, [actorPos.Position]);
            else
                cordList.Add(actorPos.Position);
        }

        return actorLocations;
    }
}
