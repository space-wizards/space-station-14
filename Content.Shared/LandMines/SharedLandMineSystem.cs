using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared.LandMines;

public abstract class SharedLandMineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LandMineComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<LandMineComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    /// <summary>
    /// Adds a verb to prime the landmine to activate if somebody steps on it.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, LandMineComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("land-mine-verb-begin"),
            Disabled = component.Armed,
            Priority = 10,
            Act = () =>
            {
                component.Armed = true;
                ChangeLandMineVisuals(uid, component);
            },
        });
    }

    private void OnExamine(EntityUid uid, LandMineComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        using (args.PushGroup(nameof(LandMineComponent)))
        {
            if(comp.Armed)
                args.PushMarkup(Loc.GetString("land-mine-armed", ("name", uid)));
        }
    }
    private void ChangeLandMineVisuals(EntityUid uid, LandMineComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, LandMineVisuals.Armed, component.Armed, appearance);
    }
}
