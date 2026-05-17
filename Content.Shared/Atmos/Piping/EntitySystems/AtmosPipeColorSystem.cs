using Content.Shared.Atmos.Piping.Components;

namespace Content.Shared.Atmos.Piping.EntitySystems;

public sealed partial class AtmosPipeColorSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeColorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AtmosPipeColorComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<AtmosPipeColorComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        _appearance.SetData(ent.Owner, PipeColorVisuals.Color, ent.Comp.Color, appearance);
    }

    private void OnShutdown(Entity<AtmosPipeColorComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        _appearance.SetData(ent.Owner, PipeColorVisuals.Color, Color.White, appearance);
    }

    public void SetColor(Entity<AtmosPipeColorComponent> ent, Color color)
    {
        ent.Comp.Color = color;

        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        _appearance.SetData(ent.Owner, PipeColorVisuals.Color, color, appearance);

        var ev = new AtmosPipeColorChangedEvent(color);
        RaiseLocalEvent(ent.Owner, ref ev);
    }
}
