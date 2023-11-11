using Content.Server.Thief.Components;
using Content.Shared.Thief;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Thief.Systems;
public sealed class ThiefUndeterminedBackpackSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefUndeterminedBackpackComponent, ThiefBackpackApproveMessage>(OnApprove);
        SubscribeLocalEvent<ThiefUndeterminedBackpackComponent, ThiefBackpackChangeSetMessage>(OnChangeSet);
    }

    private void OnApprove(Entity<ThiefUndeterminedBackpackComponent> ent, ref ThiefBackpackApproveMessage args)
    {
        Log.Debug("--- Really Approved");
        UpdateUI(ent.Owner, ent.Comp);
    }
    private void OnChangeSet(Entity<ThiefUndeterminedBackpackComponent> component, ref ThiefBackpackChangeSetMessage args)
    {
        Log.Debug("--- Really Changed" + args.SetNumber);
    }

    private void UpdateUI(EntityUid uid, ThiefUndeterminedBackpackComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        List<ThiefBackpackSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new ThiefBackpackSetInfo(
                Loc.GetString(set.Name),
                Loc.GetString(set.Description),
                set.Sprite,
                selected);
            data.Add(info);
        }

        _ui.TrySetUiState(uid, ThiefBackpackUIKey.Key, new ThiefBackpackBoundUserInterfaceState(data));
    }
}
