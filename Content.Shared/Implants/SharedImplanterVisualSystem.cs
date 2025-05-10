using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public sealed class SharedImplanterVisualSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
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
                _appearance.SetData(uid, ImplanterVisuals.Color, subcomp.Color, appearance);
            }
        }
    }
}
