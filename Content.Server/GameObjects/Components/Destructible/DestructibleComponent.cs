#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
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
        private SortedDictionary<int, Threshold> _thresholds = new SortedDictionary<int, Threshold>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, Threshold>(),
                thresholds => _thresholds = new SortedDictionary<int, Threshold>(thresholds, Comparer<int>.Create((a, b) => b.CompareTo(a))),
                () => new Dictionary<int, Threshold>(_thresholds));
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

                    // TODO LINQ
                    foreach (var (threshold, state) in _thresholds.Reverse())
                    {
                        if (msg.Damageable.TotalDamage >= threshold)
                        {
                            state.Trigger(Owner, _random, _actSystem);
                            break;
                        }
                    }

                    break;
                }
            }
        }
    }
}
