using System.Linq;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tools;

public abstract class SharedToolSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MultipleToolComponent, ComponentStartup>(OnMultipleToolStartup);
        SubscribeLocalEvent<MultipleToolComponent, ActivateInWorldEvent>(OnMultipleToolActivated);
        SubscribeLocalEvent<MultipleToolComponent, ComponentGetState>(OnMultipleToolGetState);
        SubscribeLocalEvent<MultipleToolComponent, ComponentHandleState>(OnMultipleToolHandleState);

        SubscribeLocalEvent<ToolComponent, DoAfterEvent<ToolEventData>>(OnDoAfter);

        SubscribeLocalEvent<ToolDoAfterComplete>(OnDoAfterComplete);
        SubscribeLocalEvent<ToolDoAfterCancelled>(OnDoAfterCancelled);
    }

    private void OnDoAfter(EntityUid uid, ToolComponent component, DoAfterEvent<ToolEventData> args)
    {
        if (args.Handled || args.Cancelled || args.AdditionalData.Ev == null)
            return;

        if (ToolFinishUse(uid, args.Args.User, args.AdditionalData.Fuel))
        {
            if (args.AdditionalData.TargetEntity != null)
                RaiseLocalEvent(args.AdditionalData.TargetEntity.Value, args.AdditionalData.Ev);
            else
                RaiseLocalEvent(args.AdditionalData.Ev);

            args.Handled = true;
        }
        else if (args.AdditionalData.CancelledEv != null)
        {
            if (args.AdditionalData.TargetEntity != null)
                RaiseLocalEvent(args.AdditionalData.TargetEntity.Value, args.AdditionalData.CancelledEv);
            else
                RaiseLocalEvent(args.AdditionalData.CancelledEv);

            args.Handled = true;
        }
    }

    public bool UseTool(EntityUid tool, EntityUid user, EntityUid? target, float doAfterDelay, IEnumerable<string> toolQualitiesNeeded, ToolEventData toolEventData, float fuel = 0f, ToolComponent? toolComponent = null, Func<bool>? doAfterCheck = null)
    {
        // No logging here, after all that'd mean the caller would need to check if the component is there or not.
        if (!Resolve(tool, ref toolComponent, false))
            return false;

        var ev = new ToolUserAttemptUseEvent(user, target);
        RaiseLocalEvent(user, ref ev);
        if (ev.Cancelled)
            return false;

        if (!ToolStartUse(tool, user, fuel, toolQualitiesNeeded, toolComponent))
            return false;

        if (doAfterDelay > 0f)
        {
            var doAfterArgs = new DoAfterEventArgs(user, doAfterDelay / toolComponent.SpeedModifier, target:target, used:tool)
            {
                ExtraCheck = doAfterCheck,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            };

            _doAfterSystem.DoAfter(doAfterArgs, toolEventData);
            return true;
        }

        return ToolFinishUse(tool, user, fuel, toolComponent);
    }

    public bool UseTool(EntityUid tool, EntityUid user, EntityUid? target, float doAfterDelay, string toolQualityNeeded,
        ToolEventData toolEventData, float fuel = 0, ToolComponent? toolComponent = null,
        Func<bool>? doAfterCheck = null)
    {
        return UseTool(tool, user, target, doAfterDelay, new[] { toolQualityNeeded }, toolEventData, fuel,
            toolComponent, doAfterCheck);
    }

    private void OnMultipleToolHandleState(EntityUid uid, MultipleToolComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MultipleToolComponentState state)
            return;

        component.CurrentEntry = state.Selected;
        SetMultipleTool(uid, component);
    }

    private void OnMultipleToolStartup(EntityUid uid, MultipleToolComponent multiple, ComponentStartup args)
    {
        // Only set the multiple tool if we have a tool component.
        if(EntityManager.TryGetComponent(uid, out ToolComponent? tool))
            SetMultipleTool(uid, multiple, tool);
    }

    private void OnMultipleToolActivated(EntityUid uid, MultipleToolComponent multiple, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = CycleMultipleTool(uid, multiple, args.User);
    }

    private void OnMultipleToolGetState(EntityUid uid, MultipleToolComponent multiple, ref ComponentGetState args)
    {
        args.State = new MultipleToolComponentState(multiple.CurrentEntry);
    }

    public bool CycleMultipleTool(EntityUid uid, MultipleToolComponent? multiple = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref multiple))
            return false;

        if (multiple.Entries.Length == 0)
            return false;

        multiple.CurrentEntry = (uint) ((multiple.CurrentEntry + 1) % multiple.Entries.Length);
        SetMultipleTool(uid, multiple, playSound: true, user: user);

        return true;
    }

    public virtual void SetMultipleTool(EntityUid uid,
        MultipleToolComponent? multiple = null,
        ToolComponent? tool = null,
        bool playSound = false,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref multiple, ref tool))
            return;

        Dirty(multiple);

        if (multiple.Entries.Length <= multiple.CurrentEntry)
        {
            multiple.CurrentQualityName = Loc.GetString("multiple-tool-component-no-behavior");
            return;
        }

        var current = multiple.Entries[multiple.CurrentEntry];
        tool.UseSound = current.Sound;
        tool.Qualities = current.Behavior;

        if (playSound && current.ChangeSound != null)
            _audioSystem.PlayPredicted(current.ChangeSound, uid, user);

        if (_protoMan.TryIndex(current.Behavior.First(), out ToolQualityPrototype? quality))
            multiple.CurrentQualityName = Loc.GetString(quality.Name);
    }

    /// <summary>
    ///     Whether a tool entity has the specified quality or not.
    /// </summary>
    public bool HasQuality(EntityUid uid, string quality, ToolComponent? tool = null)
    {
        return Resolve(uid, ref tool, false) && tool.Qualities.Contains(quality);
    }

    /// <summary>
    ///     Whether a tool entity has all specified qualities or not.
    /// </summary>
    public bool HasAllQualities(EntityUid uid, IEnumerable<string> qualities, ToolComponent? tool = null)
    {
        return Resolve(uid, ref tool, false) && tool.Qualities.ContainsAll(qualities);
    }


    private bool ToolStartUse(EntityUid tool, EntityUid user, float fuel, IEnumerable<string> toolQualitiesNeeded, ToolComponent? toolComponent = null)
    {
        if (!Resolve(tool, ref toolComponent))
            return false;

        if (!toolComponent.Qualities.ContainsAll(toolQualitiesNeeded))
            return false;

        var beforeAttempt = new ToolUseAttemptEvent(fuel, user);
        RaiseLocalEvent(tool, beforeAttempt, false);

        return !beforeAttempt.Cancelled;
    }

    private bool ToolFinishUse(EntityUid tool, EntityUid user, float fuel, ToolComponent? toolComponent = null)
    {
        if (!Resolve(tool, ref toolComponent))
            return false;

        var afterAttempt = new ToolUseFinishAttemptEvent(fuel, user);
        RaiseLocalEvent(tool, afterAttempt, false);

        if (afterAttempt.Cancelled)
            return false;

        if (toolComponent.UseSound != null)
            PlayToolSound(tool, toolComponent);

        return true;
    }

    public void PlayToolSound(EntityUid uid, ToolComponent? tool = null)
    {
        if (!Resolve(uid, ref tool))
            return;

        if (tool.UseSound is not {} sound)
            return;

        // Pass tool.Owner to Filter.Pvs to avoid a TryGetEntity call.
        SoundSystem.Play(sound.GetSound(), Filter.Pvs(tool.Owner),
            uid, AudioHelpers.WithVariation(0.175f).WithVolume(-5f));
    }

    private void OnDoAfterComplete(ToolDoAfterComplete ev)
    {
        // Actually finish the tool use! Depending on whether that succeeds or not, either event will be broadcast.
        if(ToolFinishUse(ev.Uid, ev.UserUid, ev.Fuel))
        {
            if (ev.EventTarget != null)
                RaiseLocalEvent(ev.EventTarget.Value, ev.CompletedEvent, false);
            else
                RaiseLocalEvent(ev.CompletedEvent);
        }
        else if(ev.CancelledEvent != null)
        {
            if (ev.EventTarget != null)
                RaiseLocalEvent(ev.EventTarget.Value, ev.CancelledEvent, false);
            else
                RaiseLocalEvent(ev.CancelledEvent);
        }
    }

    private void OnDoAfterCancelled(ToolDoAfterCancelled ev)
    {
        if (ev.EventTarget != null)
            RaiseLocalEvent(ev.EventTarget.Value, ev.Event, false);
        else
            RaiseLocalEvent(ev.Event);
    }

    private sealed class ToolDoAfterComplete : EntityEventArgs
    {
        public readonly object CompletedEvent;
        public readonly object? CancelledEvent;
        public readonly EntityUid Uid;
        public readonly EntityUid UserUid;
        public readonly float Fuel;
        public readonly EntityUid? EventTarget;

        public ToolDoAfterComplete(object completedEvent, object? cancelledEvent, EntityUid uid, EntityUid userUid, float fuel, EntityUid? eventTarget = null)
        {
            CompletedEvent = completedEvent;
            Uid = uid;
            UserUid = userUid;
            Fuel = fuel;
            CancelledEvent = cancelledEvent;
            EventTarget = eventTarget;
        }
    }

    private sealed class ToolDoAfterCancelled : EntityEventArgs
    {
        public readonly object Event;
        public readonly EntityUid? EventTarget;

        public ToolDoAfterCancelled(object @event, EntityUid? eventTarget = null)
        {
            Event = @event;
            EventTarget = eventTarget;
        }
    }
}

