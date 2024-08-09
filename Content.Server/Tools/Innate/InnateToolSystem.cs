using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Destructible;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Network;
using Robust.Shared.Random;

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

    private void OnMapInit(EntityUid uid, InnateToolComponent component, MapInitEvent args)
    {
        if (component.Tools.Count == 0)
            return;

        component.ToSpawn = EntitySpawnCollection.GetSpawns(component.Tools, _robustRandom);
    }

    private void OnHandCountChanged(EntityUid uid, InnateToolComponent component, HandCountChangedEvent args)
    {
        if (component.ToSpawn.Count == 0)
            return;

        var spawnCoord = Transform(uid).Coordinates;

        var toSpawn = component.ToSpawn.First();

        var item = Spawn(toSpawn, spawnCoord);
        AddComp<UnremoveableComponent>(item);
        if (!_sharedHandsSystem.TryPickupAnyHand(uid, item, checkActionBlocker: false))
        {
            QueueDel(item);
            component.ToSpawn.Clear();
        }
        component.ToSpawn.Remove(toSpawn);
        component.ToolUids.Add(item);
    }

    private void OnShutdown(EntityUid uid, InnateToolComponent component, ComponentShutdown args)
    {
        foreach (var tool in component.ToolUids)
        {
            RemComp<UnremoveableComponent>(tool);
        }
    }

    private void OnDestroyed(EntityUid uid, InnateToolComponent component, DestructionEventArgs args)
    {
        Cleanup(uid, component);
    }

    public void Cleanup(EntityUid uid, InnateToolComponent component)
    {
        foreach (var tool in component.ToolUids)
        {
            if (_tagSystem.HasTag(tool, "InnateDontDelete"))
            {
                RemComp<UnremoveableComponent>(tool);
            }
            else
            {
                Del(tool);
            }

            if (TryComp<HandsComponent>(uid, out var hands))
            {
                foreach (var hand in hands.Hands)
                {
                    _sharedHandsSystem.TryDrop(uid, hand.Value, checkActionBlocker: false, handsComp: hands);
                }
            }
        }

        component.ToolUids.Clear();
    }
}
