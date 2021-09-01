using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wieldable
{
    /// <summary>
    ///     Used for objects that can be wielded in two or more hands,
    /// </summary>
    [RegisterComponent, Friend(typeof(WieldableSystem))]
    public class WieldableComponent : Component
    {
        public override string Name => "Wieldable";

        [DataField("wieldSound")]
        public SoundSpecifier? WieldSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [DataField("unwieldSound")]
        public SoundSpecifier? UnwieldSound = default!;

        public bool Wielded = false;

        public string WieldedInhandPrefix = "wielded";

        public string? OldInhandPrefix = null;

        [DataField("wieldTime")]
        public float WieldTime = 0.5f;

        [Verb]
        public sealed class WieldVerb : Verb<WieldableComponent>
        {
            public override bool AlternativeInteraction => true;

            protected override void GetData(IEntity user, WieldableComponent component, VerbData data)
            {
                data.Visibility = component.Wielded ? VerbVisibility.Invisible : VerbVisibility.Visible;
                data.Text = "Wield";
            }

            protected override void Activate(IEntity user, WieldableComponent component)
            {
                EntitySystem.Get<WieldableSystem>().AttemptWield(component.Owner.Uid, component, user);
            }
        }

        [Verb]
        public sealed class UnwieldVerb : Verb<WieldableComponent>
        {
            public override bool AlternativeInteraction => true;

            protected override void GetData(IEntity user, WieldableComponent component, VerbData data)
            {
                var canWield = EntitySystem.Get<WieldableSystem>().CanWield(component.Owner.Uid, component, user);
                data.Visibility = component.Wielded ? VerbVisibility.Visible : VerbVisibility.Invisible;
                data.Text = "Unwield";
            }

            protected override void Activate(IEntity user, WieldableComponent component)
            {
                EntitySystem.Get<WieldableSystem>().AttemptUnwield(component.Owner.Uid, component, user);
            }
        }
    }
}
