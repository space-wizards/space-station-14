using Content.Shared.Acts;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Destructible
{
    [UsedImplicitly]
    public class DestructibleSystem : EntitySystem
    {
        [Dependency] public readonly IRobustRandom Random = default!;
        [Dependency] public readonly AudioSystem AudioSystem = default!;
        [Dependency] public readonly ActSystem ActSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(CheckAndExectuteThresholds);
        }

        /// <summary>
        /// Check if any thresholds were reached. if they were, execute them.
        /// </summary>
        public void CheckAndExectuteThresholds(EntityUid _, DestructibleComponent component, DamageChangedEvent args)
        {
            foreach (var threshold in component.Thresholds)
            {
                if (threshold.Reached(args.Damageable, this))
                {
                    threshold.Execute(component.Owner, this);
                }
            }
        }
    }
}
