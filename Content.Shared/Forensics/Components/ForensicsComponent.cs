using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ForensicsComponent : Component
    {
        [DataField, AutoNetworkedField]
        public HashSet<string> Fingerprints = new();

        [DataField, AutoNetworkedField]
        public HashSet<string> Fibers = new();

        [DataField, AutoNetworkedField]
        public HashSet<string> DNAs = new();

        [DataField, AutoNetworkedField]
        public HashSet<string> Residues = new();

        /// <summary>
        /// How close you must be to wipe the prints/blood/etc. off of this entity
        /// </summary>
        [DataField, AutoNetworkedField]
        public float CleanDistance = 1.5f;

        /// <summary>
        /// Can the DNA be cleaned off of this entity?
        /// e.g. you can wipe the DNA off of a knife, but not a cigarette
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool CanDnaBeCleaned = true;
    }
}
