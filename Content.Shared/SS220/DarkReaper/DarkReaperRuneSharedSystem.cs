// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DarkReaper;

public sealed class DarkReaperRuneSharedSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("dark-reaper-spawn");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperRuneComponent, ReaperSpawnEvent>(OnSpawnAction);
        SubscribeLocalEvent<DarkReaperRuneComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, DarkReaperRuneComponent component, ComponentStartup args)
    {
        if (_net.IsServer)
            _actions.AddAction(uid, ref component.SpawnActionEntity, component.SpawnAction);
    }

    private void OnSpawnAction(EntityUid uid, DarkReaperRuneComponent component, ReaperSpawnEvent args)
    {
        if (!_net.IsServer)
            return;

        args.Handled = true;

        if (!_prototype.TryIndex<EntityPrototype>(component.DarkReaperPrototypeId, out var reaperProto))
            return;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var coords = Transform(uid).Coordinates;
        if (!coords.IsValid(EntityManager))
        {
            _sawmill.Debug("Failed to spawn Dark Reaper: spawn coordinates are invalid!");
            return;
        }

        var reaper = Spawn(component.DarkReaperPrototypeId, Transform(uid).Coordinates);
        _mindSystem.TransferTo(mindId, reaper, mind: mind);
        _audio.PlayPvs(component.SpawnSound, reaper);

        QueueDel(uid);
    }
}
