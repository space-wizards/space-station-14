using Content.Shared.MapText;
using Robust.Shared.GameStates;

namespace Content.Server.MapText;

/// <inheritdoc/>
public sealed class MapTextSystem : SharedMapTextSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapTextComponent, ComponentGetState>(GetCompState);
    }

    private void GetCompState(Entity<MapTextComponent> ent, ref ComponentGetState args)
    {
        args.State = new MapTextComponentState
        {
            Text = ent.Comp.Text,
            LocText = ent.Comp.LocText,
            Color = ent.Comp.Color,
            FontId = ent.Comp.FontId,
            FontSize = ent.Comp.FontSize,
            Offset = ent.Comp.Offset
        };
    }
}
