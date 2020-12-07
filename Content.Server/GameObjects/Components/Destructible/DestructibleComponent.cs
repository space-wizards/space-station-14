#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage
    ///     and triggers thresholds when reached.
    /// </summary>
    [RegisterComponent]
    public class DestructibleComponent : Component
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private ActSystem _actSystem = default!;

        public override string Name => "Destructible";

        [ViewVariables]
        private SortedDictionary<int, Threshold> _lowestToHighestThresholds = new();

        [ViewVariables] private int PreviousTotalDamage { get; set; }

        public IReadOnlyDictionary<int, Threshold> LowestToHighestThresholds => _lowestToHighestThresholds;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, Threshold>(),
                thresholds => _lowestToHighestThresholds = new SortedDictionary<int, Threshold>(thresholds),
                () => new Dictionary<int, Threshold>(_lowestToHighestThresholds));
        }

        public override void Initialize()
        {
            base.Initialize();

            _actSystem = EntitySystem.Get<ActSystem>();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DamageChangedMessage msg:
                {
                    if (msg.Damageable.Owner != Owner)
                    {
                        break;
                    }

                    foreach (var (damage, threshold) in _lowestToHighestThresholds)
                    {
                        if (threshold.Triggered)
                        {
                            if (threshold.TriggersOnce)
                            {
                                continue;
                            }

                            if (PreviousTotalDamage >= damage)
                            {
                                continue;
                            }
                        }

                        if (msg.Damageable.TotalDamage >= damage)
                        {
                            var thresholdMessage = new DestructibleThresholdReachedMessage(this, threshold, msg.Damageable.TotalDamage, damage);
                            SendMessage(thresholdMessage);

                            threshold.Trigger(Owner, _random, _actSystem);
                        }
                    }

                    PreviousTotalDamage = msg.Damageable.TotalDamage;

                    break;
                }
            }
        }
    }
}
