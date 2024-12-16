using Content.Shared.Verbs;

namespace Content.Shared.LandMines;

public abstract class SharedLandMineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
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
            Disabled = component is {Armed:true},
            Priority = 10,
            Act = () =>
            {
                component.Armed = true;
                ChangeLandMineVisuals(uid, component);
            },
        });
    }

    private void ChangeLandMineVisuals(EntityUid uid, LandMineComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, LandMineVisuals.Armed, component.Armed, appearance);
    }
}
