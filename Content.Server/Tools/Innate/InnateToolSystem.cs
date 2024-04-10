using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Tools.Innate;

/// <summary>
///     Spawns unremovable tools in HandsComponent. Do nothing if no HandsComponent exists on enitity prototype, or if comp added in realtime. Used for drones,
///     borgs, or maybe even stuff like changeling armblades!
/// </summary>
public sealed class InnateToolSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InnateToolComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnateToolComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InnateToolComponent, ComponentRemove>(OnDestroyed);
    }

    /// <summary>
    /// Creates hands using HandsComponent (if any) and spawn in hands innate tools
    /// </summary>
    /// <param name="innateTool"></param>
    /// <param name="args"></param>
    private void OnMapInit(Entity<InnateToolComponent> innateTool, ref MapInitEvent args)
    {
        if (innateTool.Comp.Tools.Count == 0)
            return;

        if (!TryComp<HandsComponent>(innateTool.Owner, out var hands))
            return;

        innateTool.Comp.ToSpawn = EntitySpawnCollection.GetSpawns(innateTool.Comp.Tools, _robustRandom);
        AddHands(innateTool, innateTool.Comp.Tools.Count);
        TrySpawnInnateTool(innateTool, hands);
    }


    /// <summary>
    /// Creates hands
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    private void AddHands(Entity<InnateToolComponent> innateTool, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            string handId = $"{innateTool.Owner}-innateHand{i}"; // TODO: another way of unique names. If try to add second same hand name, then that second hand don't add
            _sharedHandsSystem.AddHand(innateTool.Owner, handId, HandLocation.Left);
            innateTool.Comp.HandIds.Add(handId);
        }
    }

    private void TrySpawnInnateTool(Entity<InnateToolComponent> innateTool, HandsComponent hands)
    {
        for (var i = 0; i < innateTool.Comp.ToSpawn.Count; i++)
        {
            var spawnCoord = Transform(innateTool.Owner).Coordinates;
            var toSpawn = innateTool.Comp.ToSpawn[i];
            var item = Spawn(toSpawn, spawnCoord);

            _sharedHandsSystem.DoPickup(innateTool.Owner, hands.Hands[innateTool.Comp.HandIds[i]], item);

            AddComp<UnremoveableComponent>(item);
            innateTool.Comp.ToolUids.Add(item);
        }

        innateTool.Comp.ToSpawn.Clear();
    }

    private void OnShutdown(Entity<InnateToolComponent> innateTool, ref ComponentShutdown args)
    {
        foreach (var tool in innateTool.Comp.ToolUids)
        {
            RemComp<UnremoveableComponent>(tool);
        }
    }

    private void OnDestroyed(Entity<InnateToolComponent> innateTool, ref ComponentRemove args)
    {
        Cleanup(innateTool);
    }

    private void Cleanup(Entity<InnateToolComponent> innateTool)
    {
        TryComp<HandsComponent>(innateTool, out var hands);

        int i = 0;
        foreach (var tool in innateTool.Comp.ToolUids)
        {
            RemComp<UnremoveableComponent>(tool); // in RemComp already existsd tryGetComp
            Del(tool);
            if (hands != null)
                _sharedHandsSystem.TryDrop(innateTool.Owner, hands.Hands[innateTool.Comp.HandIds[i]], checkActionBlocker: false, handsComp: hands);

            i++;
        }

        innateTool.Comp.ToolUids.Clear();
        innateTool.Comp.HandIds.Clear();
    }
}
