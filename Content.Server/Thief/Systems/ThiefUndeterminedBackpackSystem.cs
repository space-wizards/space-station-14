using Content.Server.Thief.Components;
using Content.Shared.Thief;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Thief.Systems;
public sealed class ThiefUndeterminedBackpackSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const int MaxSelectedSets = 2;
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
        Log.Debug("--- Really Approved");
    }
    private void OnChangeSet(Entity<ThiefUndeterminedBackpackComponent> backpack, ref ThiefBackpackChangeSetMessage args)
    {
        Log.Debug("--- Really Changed " + args.SetNumber);
        //Swith selecting set
        if (backpack.Comp.SelectedSets.Contains(args.SetNumber))
            backpack.Comp.SelectedSets.Remove(args.SetNumber);
        else
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

        _ui.TrySetUiState(uid, ThiefBackpackUIKey.Key, new ThiefBackpackBoundUserInterfaceState(data, MaxSelectedSets));
    }
}
