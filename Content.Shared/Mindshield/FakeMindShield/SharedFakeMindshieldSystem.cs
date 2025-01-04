using Content.Shared.Actions;
using Content.Shared.Mindshield.Components;

namespace Content.Shared.Mindshield.FakeMindShield;

public sealed class SharedFakeMindShieldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
    }

    private void OnToggleMindshield(EntityUid uid, FakeMindShieldComponent comp, FakeMindShieldToggleEvent toggleEvent)
    {
        comp.IsEnabled = !comp.IsEnabled;
        Dirty(uid, comp);
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent;
