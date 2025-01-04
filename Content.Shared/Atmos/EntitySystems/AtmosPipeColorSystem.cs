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
        if (!TryComp<AppearanceComponent>(item.Owner, out var appearance))
            return;

        _appearance.SetData(item.Owner, PipeColorVisuals.Color, item.Comp.Color, appearance);
    }

    private void OnShutdown(Entity<AtmosPipeColorComponent> item, ref ComponentShutdown args)
    {
        if (!TryComp<AppearanceComponent>(item.Owner, out var appearance))
            return;

        _appearance.SetData(item.Owner, PipeColorVisuals.Color, Color.White, appearance);
    }

    public void SetColor(Entity<AtmosPipeColorComponent> item, Color color)
    {
        item.Comp.Color = color;

        if (!TryComp<AppearanceComponent>(item.Owner, out var appearance))
            return;

        if (TryComp<AtmosPipeColorComponent>(item.Owner, out var colorSync))
        {
            if (colorSync != null)
            {
                colorSync.Color = color;
                Dirty(item.Owner, colorSync);
            }
        }

        _appearance.SetData(item.Owner, PipeColorVisuals.Color, color, appearance);
    }
}

