using Content.Shared.Examine;

namespace Content.Shared.Wires;

public abstract class SharedWiresSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WiresPanelComponent, ExaminedEvent>(OnExamine);
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

            if (component?.WiresPanelSecurityExamination != null)
            {
                args.PushMarkup(Loc.GetString(component.WiresPanelSecurityExamination));
            }
        }
    }

    /// <summary>
    /// If the entity has a wire panel, returns whether it is closed.
    /// Otherwise returns false as it has no panel to close.
    /// </summary>
    public bool IsClosed(EntityUid uid, WiresPanelComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        return !comp.Open;
    }
}
