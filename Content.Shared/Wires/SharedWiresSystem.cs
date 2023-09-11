using Content.Shared.Examine;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wires;

public abstract class SharedWiresSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

            if (_prototypeManager.TryIndex<WiresPanelSecurityLevelPrototype>(component.CurrentSecurityLevelID, out var securityLevelPrototype) &&
                securityLevelPrototype.Examine != null)
            {
                args.PushMarkup(Loc.GetString(securityLevelPrototype.Examine));
            }
        }
    }

    private void OnWeldableAttempt(EntityUid uid, WiresPanelComponent component, WeldableAttemptEvent args)
    {
        if (component.Open &&
            _prototypeManager.TryIndex<WiresPanelSecurityLevelPrototype>(component.CurrentSecurityLevelID, out var securityLevelPrototype) &&
            !securityLevelPrototype.WeldingAllowed)
        {
            args.Cancel();
        }
    }
}
