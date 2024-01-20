using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Atmos.Rotting;

public abstract class SharedRottingSystem : EntitySystem
{
    public const int MaxStages = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RottingComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Return the rot stage, usually from 0 to 2 inclusive.
    /// </summary>
    public int RotStage(EntityUid uid, RottingComponent? comp = null, PerishableComponent? perishable = null)
    {
        if (!Resolve(uid, ref comp, ref perishable))
            return 0;

        return (int) (comp.TotalRotTime.TotalSeconds / perishable.RotAfter.TotalSeconds);
    }

    private void OnExamined(EntityUid uid, RottingComponent component, ExaminedEvent args)
    {
        var stage = RotStage(uid, component);
        var description = stage switch
        {
            >= 2 => "rotting-extremely-bloated",
            >= 1 => "rotting-bloated",
            _ => "rotting-rotting"
        };
        args.PushMarkup(Loc.GetString(description, ("target", Identity.Entity(uid, EntityManager))));
    }
}
