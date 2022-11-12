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
        public int Amount;

        public CreateVaccineMessage(string disease, int amount)
        {
            Disease = disease;
            Amount = amount;
        }
    }

    /// <summary>
    ///     Just manual UI update.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class VaccinatorSyncRequestMessage : BoundUserInterfaceMessage
    {
        public VaccinatorSyncRequestMessage()
        {}
    }

    [Serializable, NetSerializable]
    public sealed class VaccineMachineUpdateState : BoundUserInterfaceState
    {
        public int Biomass;

        public int BiomassCost;

        public List<(string id, string name)> Diseases;

        public bool Locked;

        public VaccineMachineUpdateState(int biomass, int biomassCost, List<(string id, string name)> diseases, bool locked)
        {
            Biomass = biomass;
            BiomassCost = biomassCost;
            Diseases = diseases;
            Locked = locked;
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
