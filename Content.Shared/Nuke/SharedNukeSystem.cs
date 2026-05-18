using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.Nuke;

/// <summary>
/// Makes the big bomb go boom.
/// </summary>
public abstract partial class SharedNukeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeComponent, ExaminedEvent>(OnExaminedEvent);
    }

    #region Event Handlers

    private void OnExaminedEvent(Entity<NukeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExplosionTime != null)
        {
            var remaining = ent.Comp.ExplosionTime.Value - _timing.CurTime;

            using (args.PushGroup(nameof(SharedNukeSystem)))
            {
                args.PushMarkup(remaining < ent.Comp.AlertSoundTime
                                ? Loc.GetString("nuke-examine-exploding")
                                : Loc.GetString("nuke-examine-armed"));

                args.PushMarkup(Loc.GetString("nuke-examine-remaining-time", ("time", remaining.ToString(@"hh\:mm\:ss"))));
            }
        }

        if (Transform(ent).Anchored)
            args.PushMarkup(Loc.GetString("examinable-anchored"));
        else
            args.PushMarkup(Loc.GetString("examinable-unanchored"));
    }

    #endregion
}
