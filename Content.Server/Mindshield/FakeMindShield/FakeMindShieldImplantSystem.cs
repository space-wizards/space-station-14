using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mindshield.FakeMindShield;

namespace Content.Server.Mindshield.FakeMindShield;

public sealed class FakeMindShieldImplantSystem : SharedFakeMindShieldImplantSystem
{

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
