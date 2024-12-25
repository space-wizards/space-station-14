using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mindshield.FakeMindShield;
using Content.Shared.Tag;

namespace Content.Server.Mindshield.FakeMindShield;

public sealed class FakeMindShieldImplantSystem : SharedFakeMindShieldImplantSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeMindShieldImplantComponent, ImplantImplantedEvent>(ImplantCheck);
    }
    private void ImplantCheck(EntityUid uid, FakeMindShieldImplantComponent component ,ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted != null)
            EnsureComp<FakeMindShieldComponent>(ev.Implanted.Value);
    }
}
