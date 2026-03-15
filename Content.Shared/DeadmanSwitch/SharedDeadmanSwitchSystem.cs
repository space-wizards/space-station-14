using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Examine;
using Content.Shared.DoAfter;

namespace Content.Shared.DeadmanSwitch;

/// <summary>
/// System for deadman's switch behavior.
/// Handles OnUseInHand event, preventing the signaller from being triggered the normal way.
/// Instead, using it in hand arms / disarms it, and it will then trigger if dropped while armed.
/// </summary>
public abstract class SharedDeadmanSwitchSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadmanSwitchComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<DeadmanSwitchComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DeadmanSwitchComponent, DeadmanSwitchDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DeadmanSwitchComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Make the dead man's switch send out its remote signal.
    /// </summary>
    /// <param name="ent">The dead man's switch entity.</param>
    /// <param name="user">The entity responsible for triggering it, if applicable.</param>
    public virtual void Trigger(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
    }

    private void ToggleArmed(Entity<DeadmanSwitchComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Armed = !ent.Comp.Armed;
        _appearance.SetData(ent, ToggleableVisuals.Enabled, ent.Comp.Armed);
        Dirty(ent);
    }

    private void OnDropped(EntityUid uid, DeadmanSwitchComponent component, DroppedEvent args)
    {
        if (!component.Armed)
            return;

        ToggleArmed(uid);
        Trigger(uid, args.User);
    }

    private void OnUseInHand(EntityUid uid, DeadmanSwitchComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.ArmDelay, new DeadmanSwitchDoAfterEvent(), uid, target: uid);
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, DeadmanSwitchComponent component, DeadmanSwitchDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ToggleArmed(uid);
        ToggleInHandFeedback(uid, args.User);
    }

    protected virtual void ToggleInHandFeedback(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
    }

    private void OnExamined(EntityUid uid, DeadmanSwitchComponent component, ExaminedEvent args)
    {
        if (component.Armed)
        {
            args.PushMarkup(Loc.GetString("deadman-examine-armed"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("deadman-examine-disarmed"));
        }
    }
}
