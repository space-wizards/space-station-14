using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Tools.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Tools;
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
        }

        public async Task<bool> UseTool(EntityUid tool, EntityUid user, EntityUid? target, float fuel,
            float doAfterDelay, IEnumerable<string> toolQualitiesNeeded, Func<bool>? doAfterCheck = null,
            ToolComponent? toolComponent = null)
        {
            // No logging here, after all that'd mean the caller would need to check if the component is there or not.
            if (!Resolve(tool, ref toolComponent, false))
                return false;

            if (!toolComponent.Qualities.ContainsAll(toolQualitiesNeeded) || !_actionBlockerSystem.CanInteract(user))
                return false;

            var beforeAttempt = new ToolUseAttemptEvent(fuel, user);
            RaiseLocalEvent(tool, beforeAttempt, false);

            if (beforeAttempt.Cancelled)
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

            var afterAttempt = new ToolUseFinishAttemptEvent(fuel, user);
            RaiseLocalEvent(tool, afterAttempt, false);

            if (afterAttempt.Cancelled)
                return false;

            if (toolComponent.UseSound != null)
                PlayToolSound(tool, toolComponent);

            return true;
        }

        public Task<bool> UseTool(EntityUid tool, EntityUid user, EntityUid? target, float fuel,
            float doAfterDelay, string toolQualityNeeded, Func<bool>? doAfterCheck = null,
            ToolComponent? toolComponent = null)
        {
            return UseTool(tool, user, target, fuel, doAfterDelay, new [] {toolQualityNeeded}, doAfterCheck, toolComponent);
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
