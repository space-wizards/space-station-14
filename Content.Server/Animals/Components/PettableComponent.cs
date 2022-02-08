using System;
using Content.Shared.Sound;
using Content.Server.Animals.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Animals.Components
{
    [RegisterComponent, Friend(typeof(PettingSystem))]
    public sealed class PettableComponent : Component
    {
        [DataField("petDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan PetDelay = TimeSpan.FromSeconds(1.0);

        [DataField("petSound")]
        public SoundSpecifier? PetSound; // Nullable in case no path is specified by yaml

        /// <summary> How would you describe this creature's head? Up to two adjectives e.g. "soft floofy" </summary>
        [DataField("petDescription")]
        public string PetDescription = "";

        /// <summary> Chance that a petting attempt will succeed.
        /// 0 = never (always display failure popup, play no sound.)
        /// 0.5 = 50% chance (e.g. finnicky cats who don't always like to be pet.)
        /// 1 = always (e.g. friendly dogs)
        /// -1 = invalid (does not play a popup, e.g. if the petting target is deceased.)
        /// </summary>
        [DataField("petSuccessChance")]
        public float PetSuccessChance = 1.0f; // Always succeed by default, e.g. friendly dogs. A different value can be specified in the yaml.

        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan LastPetTime;
    }
}
