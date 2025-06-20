using Content.Shared.Actions;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.SalvoHud;

/// <summary>
/// This handles adding the toggle salvohud actions to the player when they wear it
/// </summary>
public sealed class SharedSalvohudSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShowMaterialCompositionIconsComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<ShowMaterialCompositionIconsComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ShowMaterialCompositionIconsComponent, ActivateSalvoHudEvent>(OnActivateSalvoHudEvent);
    }

    private void OnActivateSalvoHudEvent(Entity<ShowMaterialCompositionIconsComponent> ent, ref ActivateSalvoHudEvent args)
    {
        ent.Comp.CurrState = SalvohudScanState.In;
        ent.Comp.LastPingPos = _xform.GetWorldPosition(ent);
        _actions.SetUseDelay(ent.Comp.ActivateActionEnt, TimeSpan.FromSeconds(ent.Comp.InPeriod + ent.Comp.ActivePeriod + ent.Comp.OutPeriod + 0.25f)); //set the use delay such that the action can never be re-triggered while it's active
        args.Handled = true;
    }

    private void OnMapInitEvent(Entity<ShowMaterialCompositionIconsComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActivateActionEnt, ent.Comp.ActivateActionProtoID);
    }

    private void OnGetItemActions(Entity<ShowMaterialCompositionIconsComponent> ent, ref GetItemActionsEvent args)
    {
        //can't look through the hud if you're holding it
        if (args.SlotFlags == null)
            return;

        args.AddAction(ref ent.Comp.ActivateActionEnt, ent.Comp.ActivateActionProtoID);
    }
}
