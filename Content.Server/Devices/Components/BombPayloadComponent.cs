using Robust.Shared.GameObjects;

namespace Content.Server.Devices.Components
{
    [RegisterComponent]
    public class BombPayloadComponent : Component
    {
        public override string Name => "BombPayload";

        public const string BombPayloadContainer = "bombPayload";
        public const string ChemBombPayloadChemicalContainer1 = "bombPayloadChemContainer1";
        public const string ChemBombPayloadChemicalContainer2 = "bombPayloadChemContainer2";
    }
}
