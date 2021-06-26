#nullable enable
using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Shared class for injectors & syringes
    /// </summary>
    public class SharedInjectorComponent : Component
    {
        public override string Name => "Injector";
        public sealed override uint? NetID => ContentNetIDs.REAGENT_INJECTOR;

        /// <summary>
        /// Component data used for net updates. Used by client for item status ui
        /// </summary>
        [Serializable, NetSerializable]
        protected sealed class InjectorComponentState : ComponentState
        {
            public ReagentUnit CurrentVolume { get; }
            public ReagentUnit TotalVolume { get; }
            public InjectorToggleMode CurrentMode { get; }

            public InjectorComponentState(ReagentUnit currentVolume, ReagentUnit totalVolume, InjectorToggleMode currentMode) : base(ContentNetIDs.REAGENT_INJECTOR)
            {
                CurrentVolume = currentVolume;
                TotalVolume = totalVolume;
                CurrentMode = currentMode;
            }
        }

        public enum InjectorToggleMode
        {
            Inject,
            Draw
        }
    }
}
