using System.Linq;

namespace Content.Shared.SprayPainter.AtmosPipes;

/// <summary>
/// System for painting atmos pipes using an entity with the <see cref="AtmosPipes.AtmosPipePainterComponent"/>.
/// </summary>
public abstract class SharedAtmosPipePainterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipePainterComponent, MapInitEvent>(OnMapInit);
        Subs.BuiEvents<AtmosPipePainterComponent>(SprayPainterUiKey.Key,
            subs =>
            {
                subs.Event<AtmosPipePainterColorPickedMessage>(OnColorPicked);
            });
    }

    private void OnMapInit(Entity<AtmosPipePainterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ColorPalette.Count == 0)
            return;

        SetColor(ent, ent.Comp.ColorPalette.First().Key);
    }

    private void OnColorPicked(Entity<AtmosPipePainterComponent> ent,
        ref AtmosPipePainterColorPickedMessage args)
    {
        SetColor(ent, args.Key);
    }

    private void SetColor(Entity<AtmosPipePainterComponent> ent, string? paletteKey)
    {
        if (paletteKey == null ||
            paletteKey == ent.Comp.PickedColor ||
            !ent.Comp.ColorPalette.ContainsKey(paletteKey))
            return;

        ent.Comp.PickedColor = paletteKey;
        Dirty(ent, ent.Comp);
    }
}
