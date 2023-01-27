using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Access(typeof(UtensilSystem))]
    public sealed partial class UtensilComponent : Component
    {
        [DataField("types")]
        private UtensilType _types = UtensilType.None;

        [ViewVariables]
        public UtensilType Types
        {
            get => _types;
            set
            {
                if (_types.Equals(value))
                    return;

                _types = value;
            }
        }

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [DataField("breakChance")]
        public float BreakChance;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [DataField("breakSound")]
        public SoundSpecifier BreakSound = new SoundPathSpecifier("/Audio/Items/snap.ogg");
    }

    // If you want to make a fancy output on "wrong" composite utensil use (like: you need fork and knife)
    // There should be Dictionary I guess (Dictionary<UtensilType, string>)
    [Flags]
    public enum UtensilType : byte
    {
        None = 0,
        Fork = 1,
        Spoon = 1 << 1,
        Knife = 1 << 2
    }
}
