using Content.Server.Thief.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Thief;
using Robust.Server.GameObjects;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Thief.Systems;

/// <summary>
/// <see cref="ThiefUndeterminedBackpackComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed class ThiefUndeterminedBackpackSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefUndeterminedBackpackComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<ThiefUndeterminedBackpackComponent, ThiefBackpackApproveMessage>(OnApprove);
        SubscribeLocalEvent<ThiefUndeterminedBackpackComponent, ThiefBackpackChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<ThiefUndeterminedBackpackComponent> backpack, ref BoundUIOpenedEvent args)
    {
        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void OnApprove(Entity<ThiefUndeterminedBackpackComponent> backpack, ref ThiefBackpackApproveMessage args)
    {
        if (backpack.Comp.SelectedSets.Count != backpack.Comp.MaxSelectedSets)
            return;

        EntityUid? spawnedStorage = null;
        if (backpack.Comp.SpawnedStoragePrototype != null)
            spawnedStorage = Spawn(backpack.Comp.SpawnedStoragePrototype, _transform.GetMapCoordinates(backpack.Owner));

        foreach (var i in backpack.Comp.SelectedSets)
        {
            var set = _proto.Index(backpack.Comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                var ent = Spawn(item, _transform.GetMapCoordinates(backpack.Owner));
                if (TryComp<ItemComponent>(ent, out var itemComponent))
                {
                    if (spawnedStorage != null)
                        _storage.Insert(spawnedStorage.Value, ent, out _, playSound: false);
                    else
                        _transform.DropNextTo(ent, backpack.Owner);
                }
            }
        }

        if (spawnedStorage != null)
            _hands.TryPickupAnyHand(args.Actor, spawnedStorage.Value);

        // Play the sound on coordinates of the backpack/toolbox. The reason being, since we immediately delete it, the sound gets deleted alongside it.
        _audio.PlayPvs(backpack.Comp.ApproveSound, Transform(backpack.Owner).Coordinates);
        QueueDel(backpack);
    }
    private void OnChangeSet(Entity<ThiefUndeterminedBackpackComponent> backpack, ref ThiefBackpackChangeSetMessage args)
    {
        //Swith selecting set
        if (!backpack.Comp.SelectedSets.Remove(args.SetNumber))
            backpack.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void UpdateUI(EntityUid uid, ThiefUndeterminedBackpackComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, ThiefBackpackSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new ThiefBackpackSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, ThiefBackpackUIKey.Key, new ThiefBackpackBoundUserInterfaceState(data, component.MaxSelectedSets, component.ToolName, component.ToolDesc));
    }
}
