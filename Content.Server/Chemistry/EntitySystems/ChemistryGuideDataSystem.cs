using Content.Shared.Chemistry;

namespace Content.Server.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Net.RegisterNetMessage<MsgUpdateReagentGuideRegistry>();
    }
}
