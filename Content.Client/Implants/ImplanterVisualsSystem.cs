using Content.Shared.Implants.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants;

public sealed class ImplanterVisualsSystem : VisualizerSystem<ImplanterVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, ImplanterVisualsComponent comp, ref ComponentInit args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var component = Comp<ImplanterComponent>(uid);
        if (component.Implant != null)
        {
            if (_proto.TryIndex<EntityPrototype>(component.Implant.Value.Id, out var proto) &&
                proto.TryGetComponent<SubdermalImplantComponent>(out var subcomp))
            {
                AppearanceSystem.SetData(uid, ImplanterVisuals.Color, subcomp.Color, appearance);
                Log.Info($"Implanter {ToPrettyString(uid)} has implant {subcomp.Color}");
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, ImplanterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, appearance);
    }

    private void UpdateAppearance(EntityUid uid, ImplanterVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        AppearanceSystem.TryGetData(uid, ImplanterVisuals.Color, out var color, appearance);
        if (color == null)
        {
            Log.Warning($"ImplanterVisualsSystem: No color data found for {uid}");
            return;
        }
        UpdateSprite(sprite, (Color)color);
        Log.Info($"ImplanterVisualsSystem: Updated color to {color} for {uid}");
    }

    private void UpdateSprite(SpriteComponent spriteComponent, Color color)
    {
        spriteComponent.LayerSetColor("implantFull", color);
    }
}
