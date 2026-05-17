using Content.Shared.Examine;

namespace Content.Shared.Nuke;

/// <summary>
/// Makes the big bomb go boom.
/// </summary>
public abstract partial class SharedNukeSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NukeComponent, ExaminedEvent>(OnExaminedEvent);
    }

    #region Event Handlers

    protected virtual void OnMapInit(Entity<NukeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.RemainingTime = ent.Comp.Timer;
    }

    private void OnExaminedEvent(Entity<NukeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.PlayedAlertSound)
            args.PushMarkup(Loc.GetString("nuke-examine-exploding"));
        else if (ent.Comp.Status == NukeStatus.ARMED)
        {
            using (args.PushGroup(nameof(SharedNukeSystem)))
            {
                args.PushMarkup(Loc.GetString("nuke-examine-armed"));
                args.PushMarkup(Loc.GetString("nuke-examine-remaining-time", ("time", (int) ent.Comp.RemainingTime)));
            }
        }

        if (Transform(ent).Anchored)
            args.PushMarkup(Loc.GetString("examinable-anchored"));
        else
            args.PushMarkup(Loc.GetString("examinable-unanchored"));
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NukeComponent>();
        while (query.MoveNext(out var uid, out var nuke))
        {
            switch (nuke.Status)
            {
                case NukeStatus.ARMED:
                    TickTimer(uid, frameTime, nuke);
                    break;
                case NukeStatus.COOLDOWN:
                    TickCooldown(uid, frameTime, nuke);
                    break;
            }
        }
    }

    /// <summary>
    /// Counts down the timer.
    /// On server, this method plays music and explodes the nuke.
    /// </summary>
    protected virtual void TickTimer(EntityUid uid, float frameTime, NukeComponent? nuke = null)
    {
        if (!Resolve(uid, ref nuke))
            return;

        nuke.RemainingTime -= frameTime;
    }

    /// <summary>
    /// On server, counts down the cooldown for rearming the nuke.
    /// </summary>
    protected virtual void TickCooldown(EntityUid uid, float frameTime, NukeComponent? nuke = null)
    {
    }
}
