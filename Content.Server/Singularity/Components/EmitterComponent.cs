using System;
using System.Threading;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;


namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public class EmitterComponent : Component
    {
        [ComponentDependency] public readonly AppearanceComponent? Appearance = default;
        [ComponentDependency] public readonly AccessReaderComponent? AccessReader = default;
        [ComponentDependency] public readonly PowerConsumerComponent? PowerConsumer = default;

        public override string Name => "Emitter";

        public CancellationTokenSource? TimerCancel;

        // whether the power switch is in "on"
        [ViewVariables] public bool IsOn;
        // Whether the power switch is on AND the machine has enough power (so is actively firing)
        [ViewVariables] public bool IsPowered;

        // For the "emitter fired" sound
        public const float Variation = 0.25f;
        public const float Volume = 0.5f;
        public const float Distance = 3f;

        [ViewVariables] public int FireShotCounter;

        [ViewVariables] [DataField("fireSound")] public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Weapons/emitter.ogg");
        [ViewVariables] [DataField("boltType")] public string BoltType = "EmitterBolt";
        [ViewVariables] [DataField("powerUseActive")] public int PowerUseActive = 500;
        [ViewVariables] [DataField("fireBurstSize")] public int FireBurstSize = 3;
        [ViewVariables] [DataField("fireInterval")] public TimeSpan FireInterval = TimeSpan.FromSeconds(2);
        [ViewVariables] [DataField("fireBurstDelayMin")] public TimeSpan FireBurstDelayMin = TimeSpan.FromSeconds(2);
        [ViewVariables] [DataField("fireBurstDelayMax")] public TimeSpan FireBurstDelayMax = TimeSpan.FromSeconds(10);
    }
}
