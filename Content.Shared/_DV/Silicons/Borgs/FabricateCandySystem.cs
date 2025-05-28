using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._DV.Silicons.Borgs;

public sealed partial class FabricateCandySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FabricateCandyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FabricateCandyComponent, FabricateCandyActionEvent>(OnFabricate);
    }

    private void OnMapInit(Entity<FabricateCandyComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        foreach (var id in comp.Actions)
        {
            _actions.AddAction(uid, id);
        }
    }

    private void OnFabricate(Entity<FabricateCandyComponent> ent, ref FabricateCandyActionEvent args)
    {
        _audio.PlayPredicted(ent.Comp.FabricationSound, ent, ent);
        args.Handled = true;

        if (_net.IsClient)
            return;

        var spawned = Spawn(args.Item, Transform(ent).Coordinates);
        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(ent):player} fabricated {ToPrettyString(spawned):item}");
    }
}
