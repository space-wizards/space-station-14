using Content.Shared.Examine;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wires;

public abstract class SharedWiresSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WiresPanelComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<WiresPanelComponent, WeldableAttemptEvent>(OnWeldableAttempt);
    }

    private void OnExamine(EntityUid uid, WiresPanelComponent component, ExaminedEvent args)
    {
        if (!component.Open)
        {
            args.PushMarkup(Loc.GetString("wires-panel-component-on-examine-closed"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("wires-panel-component-on-examine-open"));

            if (TryComp<WiresPanelSecurityComponent>(uid, out var wiresPanelSecurity) &&
                wiresPanelSecurity.Examine != null)
            {
                args.PushMarkup(Loc.GetString(wiresPanelSecurity.Examine));
            }
        }
    }

    private void OnWeldableAttempt(EntityUid uid, WiresPanelComponent component, WeldableAttemptEvent args)
    {
        if (component.Open &&
            TryComp<WiresPanelSecurityComponent>(uid, out var wiresPanelSecurity) &&
            !wiresPanelSecurity.WeldingAllowed)
        {
            args.Cancel();
        }
    }
}
