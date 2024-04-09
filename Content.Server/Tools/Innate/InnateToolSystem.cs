using Content.Shared.Destructible;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Tools.Innate;

/// <summary>
///     Spawns a list unremovable tools in hands if possible. Used for drones,
///     borgs, or maybe even stuff like changeling armblades!
/// </summary>
public sealed class InnateToolSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InnateToolComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnateToolComponent, HandCountChangedEvent>(OnHandCountChanged);
        SubscribeLocalEvent<InnateToolComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InnateToolComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnMapInit(Entity<InnateToolComponent> innateTool, ref MapInitEvent args)
    {
        if (innateTool.Comp.Tools.Count == 0)
            return;

        innateTool.Comp.ToSpawn = EntitySpawnCollection.GetSpawns(innateTool.Comp.Tools, _robustRandom);
    }

    private void OnHandCountChanged(Entity<InnateToolComponent> innateTool, ref HandCountChangedEvent args)
    {
        if (innateTool.Comp.ToSpawn.Count == 0)
            return;

        var spawnCoord = Transform(innateTool.Owner).Coordinates;
        var toSpawn = innateTool.Comp.ToSpawn.First();
        var item = Spawn(toSpawn, spawnCoord);

        AddComp<UnremoveableComponent>(item);
        if (!_sharedHandsSystem.TryPickupAnyHand(innateTool.Owner, item, checkActionBlocker: false))
        {
            QueueDel(item);
            innateTool.Comp.ToSpawn.Clear();
        }
        innateTool.Comp.ToSpawn.Remove(toSpawn);
        innateTool.Comp.ToolUids.Add(item);
    }

    private void OnShutdown(Entity<InnateToolComponent> innateTool, ref ComponentShutdown args)
    {
        foreach (var tool in innateTool.Comp.ToolUids)
        {
            RemComp<UnremoveableComponent>(tool);
        }
    }

    private void OnDestroyed(Entity<InnateToolComponent> innateTool, ref DestructionEventArgs args)
    {
        Cleanup(innateTool);
    }

    public void Cleanup(Entity<InnateToolComponent> innateTool)
    {
        foreach (var tool in innateTool.Comp.ToolUids)
        {
            if (_tagSystem.HasTag(tool, "InnateDontDelete"))
            {
                RemComp<UnremoveableComponent>(tool);
            }
            else
            {
                Del(tool);
            }

            if (TryComp<HandsComponent>(innateTool.Owner, out var hands))
            {
                foreach (var hand in hands.Hands)
                {
                    _sharedHandsSystem.TryDrop(innateTool.Owner, hand.Value, checkActionBlocker: false, handsComp: hands);
                }
            }
        }

        innateTool.Comp.ToolUids.Clear();
    }
}
