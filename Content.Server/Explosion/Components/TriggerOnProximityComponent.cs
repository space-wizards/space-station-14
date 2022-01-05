using Content.Server.Construction.Components;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Content.Server.Explosion.Components
{

    /// <summary>
    /// Raises a <see cref="TriggerEvent"/> whenever an entity collides with a fixture attached to the owner of this component.
    /// </summary>
    [RegisterComponent]
    public sealed class TriggerOnProximityComponent : Component
    {
        public override string Name => "TriggerOnProximity";
        public TimeSpan NextTrigger = TimeSpan.Zero;
        public const string FixtureID  = "trigger-on-proximity-fixture";

        public HashSet<EntityUid> Colliding = new();

        /// <summary>
        /// Token that's used for the repeating proximity timer.
        /// </summary>
        public CancellationTokenSource? RepeatCancelTokenSource;

        [DataField("shape", required: true)]
        public IPhysShape Shape { get; set; } = default!;

        /// <summary>
        /// How long the the proximity trigger animation plays for.
        /// </summary>
        [ViewVariables]
        [DataField("animationDuration")]
        public float AnimationDuration = 0.3f;

        /// <summary>
        /// Whether the entity needs to be anchored for the proximity to work.
        /// </summary>
        [ViewVariables]
        [DataField("requiresAnchored")]
        public bool RequiresAnchored { get; set; } = true;

        [DataField("enabled")]
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool EnabledVV
        {
            get => Enabled;
            set
            {
                if (Enabled == value) return;
                EntitySystem.Get<TriggerSystem>().SetProximityFixture(Owner, this, value);
            }
        }

        [DataField("cooldown")]
        public float Cooldown { get; set; } = 10;

        /// <summary>
        /// If this proximity is triggered should we continually repeat it?
        /// </summary>
        [DataField("repeating")]
        internal bool Repeating = true;
    }
}
