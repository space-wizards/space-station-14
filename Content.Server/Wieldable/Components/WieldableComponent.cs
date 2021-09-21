using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wieldable.Components
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

        /// <summary>
        ///     Number of free hands required (excluding the item itself) required
        ///     to wield it
        /// </summary>
        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 1;

        public bool Wielded = false;

        public string WieldedInhandPrefix = "wielded";

        public string? OldInhandPrefix = null;

        [DataField("wieldTime")]
        public float WieldTime = 1.5f;

        [Verb]
        public sealed class ToggleWieldVerb : Verb<WieldableComponent>
        {
            protected override void GetData(IEntity user, WieldableComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Visible;
                data.Text = component.Wielded ? "Unwield" : "Wield";
            }

            protected override void Activate(IEntity user, WieldableComponent component)
            {
                if(!component.Wielded)
                    EntitySystem.Get<WieldableSystem>().AttemptWield(component.Owner.Uid, component, user);
                else
                    EntitySystem.Get<WieldableSystem>().AttemptUnwield(component.Owner.Uid, component, user);

            }
        }
    }
}
