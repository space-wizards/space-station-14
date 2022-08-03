using Robust.Shared.Containers;

namespace Content.Shared.Standing
{
    public abstract class SharedStandingSupportSystem : EntitySystem
    {
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
        [Dependency] protected readonly StandingStateSystem StandingStateSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ProvidesSupportComponent, EntInsertedIntoContainerMessage>(OnProviderInsertedIntoContainer);
            SubscribeLocalEvent<ProvidesSupportComponent, EntGotRemovedFromContainerMessage>(OnProviderGotRemovedFromContainer);

            SubscribeLocalEvent<NeedsSupportComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<NeedsSupportComponent, StandAttemptEvent>(OnStandAttempt);
        }

        private void OnProviderInsertedIntoContainer(EntityUid uid, ProvidesSupportComponent component, EntInsertedIntoContainerMessage args)
        {
            if (!TryComp<NeedsSupportComponent>(args.Container.Owner, out var needsSupport) ||
                !TryComp<StandingStateComponent>(needsSupport.Owner, out var standingState) ||
                !StandingStateSystem.IsDown(needsSupport.Owner, standingState))
                return;

            StandingStateSystem.Stand(needsSupport.Owner, standingState);
        }

        private void OnProviderGotRemovedFromContainer(EntityUid uid, ProvidesSupportComponent component, EntGotRemovedFromContainerMessage args)
        {
            if (!TryComp<NeedsSupportComponent>(args.Container.Owner, out var needsSupport))
                return;

            if (!IsSupported(needsSupport.Owner, needsSupport))
                StandingStateSystem.Down(needsSupport.Owner);
        }

        private void OnRemove(EntityUid uid, NeedsSupportComponent component, ComponentRemove args)
        {
            if (!TryComp<StandingStateComponent>(uid, out var standingState) ||
                !StandingStateSystem.IsDown(uid, standingState))
                return;

            StandingStateSystem.Stand(uid, standingState);
        }

        private void OnStandAttempt(EntityUid uid, NeedsSupportComponent component, StandAttemptEvent args)
        {
            if (!IsSupported(uid, component))
                args.Cancel();
        }

        /// <summary>
        ///     Checks if the entity is currently supported by any of its child entities.
        /// </summary>
        /// <remarks>
        ///     Only checks the containers directly on this entity. Will not traverse child entity containers.
        /// </remarks>
        public bool IsSupported(EntityUid uid, NeedsSupportComponent? needsSupport = null)
        {
            if (!Resolve(uid, ref needsSupport))
                return false;

            if (!HasComp<ContainerManagerComponent>(uid))
                return false;

            foreach(var container in ContainerSystem.GetAllContainers(uid))
                foreach (var ent in container.ContainedEntities)
                {
                    if (Deleted(ent))
                        continue;

                    if (HasComp<ProvidesSupportComponent>(ent))
                        return true;
                }

            return false;
        }
    }
}
