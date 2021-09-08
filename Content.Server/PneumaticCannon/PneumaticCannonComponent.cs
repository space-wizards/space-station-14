using System.Collections.Generic;
using Content.Shared.Sound;
using Content.Shared.Tool;
using Content.Shared.Verbs;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PneumaticCannon
{
    [RegisterComponent, Friend(typeof(PneumaticCannonSystem))]
    public class PneumaticCannonComponent : Component
    {
        public override string Name { get; } = "PneumaticCannon";

        [ViewVariables]
        public ContainerSlot GasTankSlot = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public PneumaticCannonPower Power = PneumaticCannonPower.Low;

        [ViewVariables(VVAccess.ReadWrite)]
        public PneumaticCannonFireMode Mode = PneumaticCannonFireMode.Single;

        /// <summary>
        ///     Used to fire the pneumatic cannon in intervals rather than all at the same time
        /// </summary>
        public float AccumulatedFrametime;

        public Queue<FireData> FireQueue = new();

        [DataField("fireInterval")]
        public float FireInterval = 0.1f;

        /// <summary>
        ///     Whether the pneumatic cannon should instantly fire once, or whether it should wait for the
        ///     fire interval initially.
        /// </summary>
        [DataField("instantFire")]
        public bool InstantFire = true;

        [DataField("toolModifyPower")]
        public ToolQuality ModifyPower = ToolQuality.Screwing;

        [DataField("toolModifyMode")]
        public ToolQuality ModifyMode = ToolQuality.Anchoring;

        /// <remarks>
        ///     If this value is too high it just straight up stops working for some reason
        /// </remarks>
        [DataField("throwStrength")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThrowStrength = 20.0f;

        [DataField("baseThrowRange")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseThrowRange = 8.0f;

        /// <summary>
        ///     How long to stun for if they shoot the pneumatic cannon at high power.
        /// </summary>
        [DataField("highPowerStunTime")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float HighPowerStunTime = 3.0f;

        [DataField("fireSound")]
        public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");

        [Verb]
        public sealed class EjectGasTankVerb : Verb<PneumaticCannonComponent>
        {
            public override bool RequireInteractionRange { get; } = true;

            protected override void GetData(IEntity user, PneumaticCannonComponent component, VerbData data)
            {
                if (component.GasTankSlot.ContainedEntities.Count == 0)
                {
                    data.Visibility = VerbVisibility.Disabled;
                }

                data.Text = Loc.GetString("pneumatic-cannon-component-verb-gas-tank-name");
            }

            protected override void Activate(IEntity user, PneumaticCannonComponent component)
            {
                EntitySystem.Get<PneumaticCannonSystem>().TryRemoveGasTank(component, user);
            }
        }

        [Verb]
        public sealed class EjectAllItems : Verb<PneumaticCannonComponent>
        {
            public override bool AlternativeInteraction { get; } = true;
            public override bool RequireInteractionRange { get; } = true;

            protected override void GetData(IEntity user, PneumaticCannonComponent component, VerbData data)
            {
                data.Text = Loc.GetString("pneumatic-cannon-component-verb-eject-items-name");
            }

            protected override void Activate(IEntity user, PneumaticCannonComponent component)
            {
                EntitySystem.Get<PneumaticCannonSystem>().TryEjectAllItems(component, user);
            }
        }

        public struct FireData
        {
            public IEntity User;
            public float Strength;
            public Vector2 Direction;
        }
    }

    /// <summary>
    ///     How strong the pneumatic cannon should be.
    ///     Each tier throws items farther and with more speed, but has drawbacks.
    ///     The highest power knocks the player down for a considerable amount of time.
    /// </summary>
    public enum PneumaticCannonPower : byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Len = 3 // used for length calc
    }

    /// <summary>
    ///     Whether to shoot one random item at a time, or all items at the same time.
    /// </summary>
    public enum PneumaticCannonFireMode : byte
    {
        Single = 0,
        All = 1,
        Len = 2 // used for length calc
    }
}
