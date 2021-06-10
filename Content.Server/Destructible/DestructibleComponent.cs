#nullable enable
using System.Collections.Generic;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Destructible
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage
    ///     and triggers thresholds when reached.
    /// </summary>
    [RegisterComponent]
    public class DestructibleComponent : Component
    {
        private DestructibleSystem _destructibleSystem = default!;

        public override string Name => "Destructible";

        [ViewVariables]
        [DataField("thresholds")]
        private List<Threshold> _thresholds = new();

        public IReadOnlyList<Threshold> Thresholds => _thresholds;

        public override void Initialize()
        {
            base.Initialize();

            _destructibleSystem = EntitySystem.Get<DestructibleSystem>();
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

                    foreach (var threshold in _thresholds)
                    {
                        if (threshold.Reached(msg.Damageable, _destructibleSystem))
                        {
                            var thresholdMessage = new DestructibleThresholdReachedMessage(this, threshold);
                            SendMessage(thresholdMessage);

                            threshold.Execute(Owner, _destructibleSystem);
                        }
                    }

                    break;
                }
            }
        }
    }
}
