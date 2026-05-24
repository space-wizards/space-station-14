using Content.Server.Implants.Components;

namespace Content.Server.Implants;

public sealed partial class AutoImplantSystem : EntitySystem
{
    [Dependency] private SubdermalImplantSystem _subdermalImplant = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoImplantComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, AutoImplantComponent comp, MapInitEvent args)
    {
        _subdermalImplant.AddImplants(uid, comp.Implants);
        RemComp<AutoImplantComponent>(uid);
    }
}
