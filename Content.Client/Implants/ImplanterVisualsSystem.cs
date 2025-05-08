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
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, ImplanterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, args.Component);
    }

    private void UpdateAppearance(EntityUid uid, ImplanterVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        AppearanceSystem.TryGetData<Color>(uid, ImplanterVisuals.Color, out var color, appearance);
        sprite.LayerSetColor("implantFull", color);
    }
}
