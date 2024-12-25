using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Tag;

namespace Content.Shared.Mindshield.FakeMindShield;

public abstract class SharedFakeMindShieldImplantSystem : EntitySystem
{
    [Dependency] public readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, FakeMindShieldToggleEvent>(OnFakeMindShieldToggle);
    }
    private void OnFakeMindShieldToggle(EntityUid uid,
        SubdermalImplantComponent component,
        FakeMindShieldToggleEvent ev)
    {
        ev.Handled = true;
        if (component.ImplantedEntity is not { } ent)
            return;

        if (TryComp<FakeMindShieldComponent>(ent, out var comp))
        {
            _actionsSystem.SetToggled(ev.Action, !comp.IsEnabled); // Set it to what the Mindshield component WILL be after this
            RaiseLocalEvent(ent, ev);
        }
    }}
