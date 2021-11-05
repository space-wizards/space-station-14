using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Tools.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Tools
{
    public partial class ToolSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            InitializeWelders();
            InitializeMultipleTools();

            SubscribeLocalEvent<ToolDoAfterComplete>(OnDoAfterComplete);
            SubscribeLocalEvent<ToolDoAfterCancelled>(OnDoAfterCancelled);
        }

        private void OnDoAfterComplete(ToolDoAfterComplete ev)
        {
            // Actually finish the tool use! Depending on whether that succeeds or not, either event will be broadcast.
            if(ToolFinishUse(ev.Uid, ev.UserUid, ev.Fuel))
                RaiseLocalEvent(ev.CompletedEvent);
            else if(ev.CancelledEvent != null)
                RaiseLocalEvent(ev.CancelledEvent);
        }

        private void OnDoAfterCancelled(ToolDoAfterCancelled ev)
        {
            // Broadcast wrapped event.
            RaiseLocalEvent(ev.Event);
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

        /// <summary>
        ///     Sync version of UseTool.
        /// </summary>
        /// <param name="tool">The tool entity.</param>
        /// <param name="user">The entity using the tool.</param>
        /// <param name="target">Optionally, a target to use the tool on.</param>
        /// <param name="fuel">An optional amount of fuel or energy to consume-</param>
        /// <param name="doAfterDelay">A doAfter delay in seconds.</param>
        /// <param name="toolQualitiesNeeded">The tool qualities needed to use the tool.</param>
        /// <param name="doAfterCompleteEvent">An event to broadcast once the doAfter is completed successfully.</param>
        /// <param name="doAfterCancelledEvent">An event to broadcast once the doAfter is cancelled.</param>
        /// <param name="doAfterCheck">An optional check to perform for the doAfter.</param>
        /// <param name="toolComponent">The tool component.</param>
        /// <returns>Whether initially, using the tool succeeded. If there's a doAfter delay, you'll need to listen to
        ///          the <see cref="doAfterCompleteEvent"/> and <see cref="doAfterCancelledEvent"/> being broadcast
        ///          to see whether using the tool succeeded or not. If the <see cref="doAfterDelay"/> is zero,
        ///          this simply returns whether using the tool succeeded or not.</returns>
        public bool UseTool(EntityUid tool, EntityUid user, EntityUid? target, float fuel,
            float doAfterDelay, IEnumerable<string> toolQualitiesNeeded,
            object? doAfterCompleteEvent = null, object? doAfterCancelledEvent = null,
            Func<bool>? doAfterCheck = null, ToolComponent? toolComponent = null)
        {
            // No logging here, after all that'd mean the caller would need to check if the component is there or not.
            if (!Resolve(tool, ref toolComponent, false))
                return false;

            if (!ToolStartUse(tool, user, fuel, toolQualitiesNeeded, toolComponent))
                return false;

            if (doAfterDelay > 0f)
            {
                var doAfterArgs = new DoAfterEventArgs(user, doAfterDelay / toolComponent.SpeedModifier, default, target)
                {
                    ExtraCheck = doAfterCheck,
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    NeedHand = true,
                    BroadcastFinishedEvent = doAfterCompleteEvent != null ? new ToolDoAfterComplete(doAfterCompleteEvent, doAfterCancelledEvent, tool, user, fuel) : null,
                    BroadcastCancelledEvent = doAfterCancelledEvent != null ? new ToolDoAfterCancelled(doAfterCancelledEvent) : null,
                };

                _doAfterSystem.DoAfter(doAfterArgs);
                return true;
            }

            return ToolFinishUse(tool, user, fuel, toolComponent);
        }

        // This is hilariously long.
        /// <inheritdoc cref="UseTool(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.EntityUid,System.Nullable{Robust.Shared.GameObjects.EntityUid},float,float,System.Collections.Generic.IEnumerable{string},Robust.Shared.GameObjects.EntityUid,object,object,System.Func{bool}?,Content.Server.Tools.Components.ToolComponent?)"/>
        public bool UseTool(EntityUid tool, EntityUid user, EntityUid? target, float fuel,
            float doAfterDelay, string toolQualityNeeded, object doAfterCompleteEvent, object doAfterCancelledEvent,
            Func<bool>? doAfterCheck = null, ToolComponent? toolComponent = null)
        {
            return UseTool(tool, user, target, fuel, doAfterDelay, new[] { toolQualityNeeded },
                doAfterCompleteEvent, doAfterCancelledEvent, doAfterCheck, toolComponent);
        }

        /// <summary>
        ///     Async version of UseTool.
        /// </summary>
        /// <param name="tool">The tool entity.</param>
        /// <param name="user">The entity using the tool.</param>
        /// <param name="target">Optionally, a target to use the tool on.</param>
        /// <param name="fuel">An optional amount of fuel or energy to consume-</param>
        /// <param name="doAfterDelay">A doAfter delay in seconds.</param>
        /// <param name="toolQualitiesNeeded">The tool qualities needed to use the tool.</param>
        /// <param name="doAfterCheck">An optional check to perform for the doAfter.</param>
        /// <param name="toolComponent">The tool component.</param>
        /// <returns>Whether using the tool succeeded or not.</returns>
        public async Task<bool> UseTool(EntityUid tool, EntityUid user, EntityUid? target, float fuel,
            float doAfterDelay, IEnumerable<string> toolQualitiesNeeded, Func<bool>? doAfterCheck = null,
            ToolComponent? toolComponent = null)
        {
            // No logging here, after all that'd mean the caller would need to check if the component is there or not.
            if (!Resolve(tool, ref toolComponent, false))
                return false;

            if (!ToolStartUse(tool, user, fuel, toolQualitiesNeeded, toolComponent))
                return false;

            if (doAfterDelay > 0f)
            {
                var doAfterArgs = new DoAfterEventArgs(user, doAfterDelay / toolComponent.SpeedModifier, default, target)
                {
                    ExtraCheck = doAfterCheck,
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    NeedHand = true,
                };

                var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);

                if (result == DoAfterStatus.Cancelled)
                    return false;
            }

            return ToolFinishUse(tool, user, fuel, toolComponent);
        }

        // This is hilariously long.
        /// <inheritdoc cref="UseTool(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.EntityUid,System.Nullable{Robust.Shared.GameObjects.EntityUid},float,float,System.Collections.Generic.IEnumerable{string},Robust.Shared.GameObjects.EntityUid,object,object,System.Func{bool}?,Content.Server.Tools.Components.ToolComponent?)"/>
        public Task<bool> UseTool(EntityUid tool, EntityUid user, EntityUid? target, float fuel,
            float doAfterDelay, string toolQualityNeeded, Func<bool>? doAfterCheck = null,
            ToolComponent? toolComponent = null)
        {
            return UseTool(tool, user, target, fuel, doAfterDelay, new [] {toolQualityNeeded}, doAfterCheck, toolComponent);
        }

        private bool ToolStartUse(EntityUid tool, EntityUid user, float fuel, IEnumerable<string> toolQualitiesNeeded, ToolComponent? toolComponent = null)
        {
            if (!Resolve(tool, ref toolComponent))
                return false;

            if (!toolComponent.Qualities.ContainsAll(toolQualitiesNeeded) || !_actionBlockerSystem.CanInteract(user))
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
            SoundSystem.Play(Filter.Pvs(tool.Owner), sound.GetSound(), uid,
                AudioHelpers.WithVariation(0.175f).WithVolume(-5f));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateWelders(frameTime);
        }

        private class ToolDoAfterComplete : EntityEventArgs
        {
            public readonly object CompletedEvent;
            public readonly object? CancelledEvent;
            public readonly EntityUid Uid;
            public readonly EntityUid UserUid;
            public readonly float Fuel;

            public ToolDoAfterComplete(object completedEvent, object? cancelledEvent, EntityUid uid, EntityUid userUid, float fuel)
            {
                CompletedEvent = completedEvent;
                Uid = uid;
                UserUid = userUid;
                Fuel = fuel;
                CancelledEvent = cancelledEvent;
            }
        }

        private class ToolDoAfterCancelled : EntityEventArgs
        {
            public readonly object Event;

            public ToolDoAfterCancelled(object @event)
            {
                Event = @event;
            }
        }
    }

    /// <summary>
    ///     Attempt event called *before* any do afters to see if the tool usage should succeed or not.
    ///     You can change the fuel consumption by changing the Fuel property.
    /// </summary>
    public class ToolUseAttemptEvent : CancellableEntityEventArgs
    {
        public float Fuel { get; set; }
        public EntityUid User { get; }

        public ToolUseAttemptEvent(float fuel, EntityUid user)
        {
            Fuel = fuel;
            User = user;
        }
    }

    /// <summary>
    ///     Attempt event called *after* any do afters to see if the tool usage should succeed or not.
    ///     You can use this event to consume any fuel needed.
    /// </summary>
    public class ToolUseFinishAttemptEvent : CancellableEntityEventArgs
    {
        public float Fuel { get; }
        public EntityUid User { get; }

        public ToolUseFinishAttemptEvent(float fuel, EntityUid user)
        {
            Fuel = fuel;
        }
    }
}
