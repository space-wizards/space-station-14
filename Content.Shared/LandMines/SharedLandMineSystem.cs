using Content.Shared.Verbs;

namespace Content.Shared.LandMines;

public abstract class SharedLandMineSystem : EntitySystem
{
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
            Disabled = IsArmed(component),
            Priority = 10,
            Act = () =>
            {
                Arm(uid, component);
            },
        });
    }

    public abstract void Arm(EntityUid uid, LandMineComponent component);

    public abstract bool IsArmed(LandMineComponent component);
}
