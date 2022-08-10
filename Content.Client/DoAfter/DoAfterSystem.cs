using System.Linq;
using Content.Client.DoAfter.UI;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.DoAfter
{
    /// <summary>
    /// Handles events that need to happen after a certain amount of time where the event could be cancelled by factors
    /// such as moving.
    /// </summary>
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
    /*
     * How this is currently setup (client-side):
     * DoAfterGui handles the actual bars displayed above heads. It also uses FrameUpdate to flash cancellations
     * DoAfterEntitySystem handles checking predictions every tick as well as removing / cancelling DoAfters due to time elapsed.
     * DoAfterComponent handles network messages inbound as well as storing the DoAfter data.
     *     It'll also handle overall cleanup when one is removed (i.e. removing it from DoAfterGui).
    */
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
        [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

        /// <summary>
        ///     We'll use an excess time so stuff like finishing effects can show.
        /// </summary>
        public const float ExcessTime = 0.5f;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            SubscribeNetworkEvent<CancelledDoAfterMessage>(OnCancelledDoAfter);
            SubscribeLocalEvent<DoAfterComponent, ComponentStartup>(OnDoAfterStartup);
            SubscribeLocalEvent<DoAfterComponent, ComponentShutdown>(OnDoAfterShutdown);
            SubscribeLocalEvent<DoAfterComponent, ComponentHandleState>(OnDoAfterHandleState);
        }

        private void OnDoAfterHandleState(EntityUid uid, DoAfterComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not DoAfterComponentState state)
                return;

            var toRemove = new RemQueue<ClientDoAfter>();

            foreach (var (id, doAfter) in component.DoAfters)
            {
                var found = false;

                foreach (var clientdoAfter in state.DoAfters)
                {
                    if (clientdoAfter.ID == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toRemove.Add(doAfter);
                }
            }

            foreach (var doAfter in toRemove)
            {
                Remove(component, doAfter);
            }

            foreach (var doAfter in state.DoAfters)
            {
                if (component.DoAfters.ContainsKey(doAfter.ID))
                    continue;

                component.DoAfters.Add(doAfter.ID, doAfter);
            }

            if (component.Gui == null || component.Gui.Disposed)
                return;

            foreach (var (_, doAfter) in component.DoAfters)
            {
                component.Gui.AddDoAfter(doAfter);
            }
        }

        private void OnDoAfterStartup(EntityUid uid, DoAfterComponent component, ComponentStartup args)
        {
            Enable(component);
        }

        private void OnDoAfterShutdown(EntityUid uid, DoAfterComponent component, ComponentShutdown args)
        {
            Disable(component);
        }

        private void OnCancelledDoAfter(CancelledDoAfterMessage ev)
        {
            if (!TryComp<DoAfterComponent>(ev.Uid, out var doAfter)) return;

            Cancel(doAfter, ev.ID);
        }

        /// <summary>
        ///     For handling PVS so we dispose of controls if they go out of range
        /// </summary>
        public void Enable(DoAfterComponent component)
        {
            if (component.Gui?.Disposed == false)
                return;

            component.Gui = new DoAfterGui {AttachedEntity = component.Owner};

            foreach (var (_, doAfter) in component.DoAfters)
            {
                component.Gui.AddDoAfter(doAfter);
            }

            foreach (var (_, cancelled) in component.CancelledDoAfters)
            {
                component.Gui.CancelDoAfter(cancelled.ID);
            }
        }

        public void Disable(DoAfterComponent component)
        {
            component.Gui?.Dispose();
            component.Gui = null;
        }

        /// <summary>
        ///     Remove a DoAfter without showing a cancellation graphic.
        /// </summary>
        /// <param name="clientDoAfter"></param>
        public void Remove(DoAfterComponent component, ClientDoAfter clientDoAfter)
        {
            component.DoAfters.Remove(clientDoAfter.ID);

            var found = false;

            for (var i = component.CancelledDoAfters.Count - 1; i >= 0; i--)
            {
                var cancelled = component.CancelledDoAfters[i];

                if (cancelled.Message == clientDoAfter)
                {
                    component.CancelledDoAfters.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found)
                component.DoAfters.Remove(clientDoAfter.ID);

            component.Gui?.RemoveDoAfter(clientDoAfter.ID);
        }

        /// <summary>
        ///     Mark a DoAfter as cancelled and show a cancellation graphic.
        /// </summary>
        ///     Actual removal is handled by DoAfterEntitySystem.
        public void Cancel(DoAfterComponent component, byte id)
        {
            foreach (var (_, cancelled) in component.CancelledDoAfters)
            {
                if (cancelled.ID == id)
                    return;
            }

            if (!component.DoAfters.ContainsKey(id))
                return;

            var doAfterMessage = component.DoAfters[id];
            component.CancelledDoAfters.Add((_gameTiming.CurTime, doAfterMessage));
            component.Gui?.CancelDoAfter(id);
        }

        // TODO move this to an overlay
        // TODO separate DoAfter & ActiveDoAfter components for the entity query.
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var currentTime = _gameTiming.CurTime;
            var attached = _player.LocalPlayer?.ControlledEntity;

            // Can't see any I guess?
            if (attached == null || Deleted(attached))
                return;

            // ReSharper disable once ConvertToLocalFunction
            var predicate = static (EntityUid uid, (EntityUid compOwner, EntityUid? attachedEntity) data)
                => uid == data.compOwner || uid == data.attachedEntity;

            var occluded = _examineSystem.IsOccluded(attached.Value);
            var viewbox = _eyeManager.GetWorldViewport().Enlarged(2.0f);
            var xforms = GetEntityQuery<TransformComponent>();
            var entXform = xforms.GetComponent(attached.Value);
            var playerPos = _xformSystem.GetWorldPosition(entXform, xforms);

            foreach (var (comp, xform) in EntityManager.EntityQuery<DoAfterComponent, TransformComponent>(true))
            {
                var doAfters = comp.DoAfters;

                if (doAfters.Count == 0 || xform.MapID != entXform.MapID)
                {
                    Disable(comp);
                    continue;
                }

                var compPos = _xformSystem.GetWorldPosition(xform, xforms);

                if (!viewbox.Contains(compPos))
                {
                    Disable(comp);
                    continue;
                }

                var range = (compPos - playerPos).Length + 0.01f;

                if (occluded &&
                    comp.Owner != attached &&
                    // Static ExamineSystemShared.InRangeUnOccluded has to die.
                    !ExamineSystemShared.InRangeUnOccluded(
                        new(playerPos, entXform.MapID),
                        new(compPos, entXform.MapID), range,
                        (comp.Owner, attached), predicate,
                        entMan: EntityManager))
                {
                    Disable(comp);
                    continue;
                }

                Enable(comp);

                var userGrid = xform.Coordinates;
                var toRemove = new RemQueue<ClientDoAfter>();

                // Check cancellations / finishes
                foreach (var (id, doAfter) in doAfters)
                {
                    var elapsedTime = (currentTime - doAfter.StartTime).TotalSeconds;

                    // If we've passed the final time (after the excess to show completion graphic) then remove.
                    if (elapsedTime > doAfter.Delay + ExcessTime)
                    {
                        toRemove.Add(doAfter);
                        continue;
                    }

                    // Don't predict cancellation if it's already finished.
                    if (elapsedTime > doAfter.Delay)
                        continue;

                    // Predictions
                    if (doAfter.BreakOnUserMove)
                    {
                        if (!userGrid.InRange(EntityManager, doAfter.UserGrid, doAfter.MovementThreshold))
                        {
                            Cancel(comp, id);
                            continue;
                        }
                    }

                    if (doAfter.BreakOnTargetMove)
                    {
                        if (!EntityManager.Deleted(doAfter.Target) &&
                            !Transform(doAfter.Target.Value).Coordinates.InRange(EntityManager, doAfter.TargetGrid,
                                doAfter.MovementThreshold))
                        {
                            Cancel(comp, id);
                            continue;
                        }
                    }
                }

                foreach (var doAfter in toRemove)
                {
                    Remove(comp, doAfter);
                }

                var count = comp.CancelledDoAfters.Count;
                // Remove cancelled DoAfters after ExcessTime has elapsed
                for (var i = count - 1; i >= 0; i--)
                {
                    var cancelled = comp.CancelledDoAfters[i];
                    if ((currentTime - cancelled.CancelTime).TotalSeconds > ExcessTime)
                    {
                        Remove(comp, cancelled.Message);
                    }
                }
            }
        }
    }
}
