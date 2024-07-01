using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Item
{
    /// <summary>
    ///     Adds ItemComponent to entity when it dies. Remove when it revives.
    /// </summary>
    public sealed class MakeItemOnDeadSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MakeItemOnDeadComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnMobStateChanged(EntityUid uid, MakeItemOnDeadComponent component, MobStateChangedEvent args)
        {
            if(!TryComp<MobStateComponent>(uid, out var mobState))
            return;

            if (mobState.CurrentState == MobState.Dead)
            {
                EnsureComp<ItemComponent>(uid);
            }
            else
            {
                EntityManager.RemoveComponent<ItemComponent>(uid);
            }
        }
    }
}
