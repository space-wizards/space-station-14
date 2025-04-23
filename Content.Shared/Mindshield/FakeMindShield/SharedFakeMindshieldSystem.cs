using Content.Shared.Actions;
using Content.Shared.Mindshield.Components;

namespace Content.Shared.Mindshield.FakeMindShield;

public sealed class SharedFakeMindShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
    }

    private void OnToggleMindshield(EntityUid uid, FakeMindShieldComponent comp, FakeMindShieldToggleEvent toggleEvent)
    {
        _actionsSystem.SetToggled(toggleEvent.Action, !comp.IsEnabled); // Set it to what the Mindshield component WILL be after this
        comp.IsEnabled = !comp.IsEnabled;
        Dirty(uid, comp);
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent;
