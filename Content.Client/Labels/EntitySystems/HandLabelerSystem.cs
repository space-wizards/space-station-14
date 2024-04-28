using Content.Client.Labels.UI;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Client.Labels.EntitySystems;

public sealed class HandLabelerSystem : SharedHandLabelerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandLabelerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<HandLabelerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not HandLabelerComponentState state)
            return;

        ent.Comp.AssignedLabel = state.AssignedLabel;
        ent.Comp.MaxLabelChars = state.MaxLabelChars;

        if (!UserInterfaceSystem.TryGetOpenUi(ent.Owner, HandLabelerUiKey.Key, out var bui) ||
            bui is not HandLabelerBoundUserInterface cBui)
        {
            return;
        }

        cBui.Reload();
    }
}
