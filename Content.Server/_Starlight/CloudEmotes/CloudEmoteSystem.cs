using Robust.Server.GameObjects;
using Robust.Shared.Spawners;
using Content.Shared._Starlight.CloudEmote;
using Robust.Shared.Map;

namespace Content.Server._Starlight.CloudEmotes;

public sealed class CloudEmoteSystem : SharedCloudEmoteSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    private float offset = 0.7f;

    public const string EmpPulseEffectPrototype = "EffectEmpPulse";
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CloudEmotePhaseComponent, TimedDespawnEvent>(OnChangePhase); 
        _sawmill = Logger.GetSawmill("cloud_emotes_server");
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
            _sawmill.Info(comp.Emote.ToString());
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
            Dirty(player.Value, comp);
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
            .Offset(0, 0.7f);
        comp.Emote = Spawn(phase_entity_to_spawn, coords);
        var phase_comp = _entMan.GetComponent<CloudEmotePhaseComponent>(comp.Emote);
        phase_comp.Player = player;
        Dirty(comp.Emote, phase_comp);
        Dirty(player, comp);
    }

    private void update_position(EntityUid player, EntityUid emote)
    {
        var position = _transformSystem.GetWorldPosition(player);
        _transformSystem.SetWorldPosition(emote, position);
    }
}