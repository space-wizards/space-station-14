using Robust.Shared.Serialization;

namespace Content.Shared.Disease.Components
{
    /// <summary>
    ///     As of now, the disease server and R&D server are one and the same.
    ///     So, this is a research client that can look for DiseaseServer
    ///     on its connected server and print vaccines of the diseases stored there.
    /// </summary>
    [RegisterComponent]
    public sealed class DiseaseVaccineCreatorComponent : Component
    {
        public DiseaseServerComponent? DiseaseServer = null;

        /// <summary>
        /// Biomass cost per vaccine.
        /// </summary>
        [DataField("BiomassCost")]
        public int BiomassCost = 4;
    }

    [NetSerializable, Serializable]
    public enum VaccineMachineUiKey : byte
    {
        Key,
    }

    /// <summary>
    ///     Sent to the server when the client chooses a vaccine to print.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CreateVaccineMessage : BoundUserInterfaceMessage
    {
        public DiseasePrototype Disease;

        public CreateVaccineMessage(DiseasePrototype disease)
        {
            Disease = disease;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VaccineMachineUpdateState : BoundUserInterfaceState
    {
        public int Biomass;

        public List<DiseasePrototype> Diseases;

        public VaccineMachineUpdateState(int biomass, List<DiseasePrototype> diseases)
        {
            Biomass = biomass;
            Diseases = diseases;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VaccineMachineBoundInterfaceState : BoundUserInterfaceState
    {}

    /// <summary>
    ///     Sent to the server to open the ResearchClient UI.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class VaccinatorServerSelectionMessage : BoundUserInterfaceMessage { }
}
