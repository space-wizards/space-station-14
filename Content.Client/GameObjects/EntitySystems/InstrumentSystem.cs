using Content.Client.GameObjects.Components.Instruments;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     This system is used to trigger an update on all instruments, and send queued midi events.
    /// </summary>
    internal sealed class InstrumentSystem : EntitySystem
    {
        private readonly IEntityQuery _instrumentQuery;

        public InstrumentSystem()
        {
            _instrumentQuery = new TypeEntityQuery(typeof(InstrumentComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in EntityManager.GetEntities(_instrumentQuery))
            {
                entity.GetComponent<InstrumentComponent>().Update();
            }
        }
    }
}
