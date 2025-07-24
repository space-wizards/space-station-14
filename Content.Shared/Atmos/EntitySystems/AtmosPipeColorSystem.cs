using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;

namespace Content.Shared.Atmos.EntitySystems;

public sealed class AtmosPipeColorSystem : EntitySystem
{

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeColorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AtmosPipeColorComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<AtmosPipeColorComponent> item, ref ComponentStartup args)
    {
        _appearance.SetData(item.Owner, PipeColorVisuals.Color, item.Comp.Color);
    }

    private void OnShutdown(Entity<AtmosPipeColorComponent> item, ref ComponentShutdown args)
    {
        _appearance.SetData(item.Owner, PipeColorVisuals.Color, Color.White);
    }

    public void SetColor(Entity<AtmosPipeColorComponent> item, Color color)
    {
        item.Comp.Color = color;
        _appearance.SetData(item.Owner, PipeColorVisuals.Color, color);
        Dirty(item);
    }
}

