using Robust.Shared.Serialization;

namespace Content.Shared.Disease.Components
{
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
        public string Disease;

        public CreateVaccineMessage(string disease)
        {
            Disease = disease;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VaccineMachineUpdateState : BoundUserInterfaceState
    {
        public int Biomass;

        public List<(string id, string name)> Diseases;

        public VaccineMachineUpdateState(int biomass, List<(string id, string name)> diseases)
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
