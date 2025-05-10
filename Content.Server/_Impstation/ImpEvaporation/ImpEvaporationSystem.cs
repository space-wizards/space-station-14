using Content.Shared._Impstation.ImpEvaporation;

namespace Content.Server._Impstation.ImpEvaporation;

/// <summary>
/// <inheritdoc/>
/// </summary>
public sealed partial class ImpEvaporationSystem : SharedImpEvaporationSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        TickEvaporation();
    }
}
