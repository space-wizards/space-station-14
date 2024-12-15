using Robust.Server.GameObjects;
using Robust.Shared.Spawners;
using Content.Shared._Starlight.CloudEmote;

namespace Content.Server._Starlight.CloudEmotes;

public sealed class CloudEmoteSystem : SharedCloudEmoteSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    private float offset = 0.7f;

    public const string EmpPulseEffectPrototype = "EffectEmpPulse";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CloudEmotePhaseComponent, TimedDespawnEvent>(OnChangePhase); 
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CloudEmoteActiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Phase == -1) { // Called only one time. This segment better be moved to OnAddedComponentEvent<CloudEmoteActiveComponent> if this thing exists
                comp.Phase += 1;
                display("CloudEmoteStart", uid, comp);
            }

           // update_position(uid, comp.Emote); // Not the best solution, better use parenting like transformSystem.SetCoordinates        
        }

    }

    private void OnChangePhase(EntityUid uid, CloudEmotePhaseComponent phase_comp, ref TimedDespawnEvent args)
    {
        EntityUid? player = phase_comp.Player;
        if (player == null) return;
        var comp = _entMan.GetComponent<CloudEmoteActiveComponent>(player.Value);
        if (comp.Phase == 2)
        {
            _entMan.RemoveComponent<CloudEmoteActiveComponent>(player.Value);
            return;
        }
        

        string phase_entity_to_spawn = "";
        comp.Phase += 1;
        switch (comp.Phase) 
        {
            case 1:
                phase_entity_to_spawn = comp.EmoteName;
                // TODO: play sound at phase 1
                break;
            case 2:
                phase_entity_to_spawn = "CloudEmoteEnd";
                break;
        }

        display(phase_entity_to_spawn, player.Value, comp);
        
    }

    private void display(string phase_entity_to_spawn, EntityUid player, CloudEmoteActiveComponent comp)
    {
        var coords = _transformSystem
            .GetMapCoordinates(player)
            .Offset(0, 0.7);
        comp.Emote = Spawn(phase_entity_to_spawn, coords);
        _entMan.GetComponent<CloudEmotePhaseComponent>(comp.Emote).Player = player;
        //update_position(player, comp.Emote);
    }

    private void update_position(EntityUid player, EntityUid emote)
    {
        var position = _transformSystem.GetWorldPosition(player);
        _transformSystem.SetWorldPosition(emote, position);
    }
}