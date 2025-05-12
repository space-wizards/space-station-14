using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;


namespace Content.Shared.Clothing.Components
{
    /// <summary>
    /// Handle the hails (audible orders to stop) coming from a security gas mask / swat mask
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SecurityHailerComponent : Component
    {
        /// <summary>
        /// Range value.
        /// </summary>
        [DataField]
        public float Distance = 0;

        /// <summary>
        /// How aggresive are the orders coming from the hailer ? Higher means more aggressive / shitsec
        /// </summary>
        public enum AggresionState
        {
            Low,
            Medium,
            High,
            Emag
        }

        [DataField]
        public AggresionState AggresionLevel = AggresionState.Low;

        [DataField]
        public SoundSpecifier LowAggressionSounds = new SoundCollectionSpecifier("SecHailLow");

        [DataField]
        public SoundSpecifier MediumAggressionSounds = new SoundCollectionSpecifier("SecHailMedium");

        [DataField]
        public SoundSpecifier HighAggressionSounds = new SoundCollectionSpecifier("SecHailHigh");

        [DataField]
        public SoundSpecifier EmagAggressionSounds = new SoundCollectionSpecifier("SecHailEmag");
    }
}
