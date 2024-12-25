using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Tag;
using Content.Shared.Toggleable;

namespace Content.Shared.Mindshield.FakeMindShield;

public abstract class SharedFakeMindShieldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
    }


    public void OnToggleMindshield(EntityUid uid, FakeMindShieldComponent comp, FakeMindShieldToggleEvent toggleEvent)
    {
        comp.IsEnabled = !comp.IsEnabled;
        Dirty(uid, comp);
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent { }
