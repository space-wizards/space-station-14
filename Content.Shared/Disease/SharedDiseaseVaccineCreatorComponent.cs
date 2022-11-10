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
        public DiseasePrototype Disease;

        public CreateVaccineMessage(DiseasePrototype disease)
        {
            Disease = disease;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VaccineMachineBoundInterfaceState : BoundUserInterfaceState
    {}
}
