using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Operation.Step;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Surgery.Systems;

public abstract class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        foreach (var operation in _proto.EnumeratePrototypes<SurgeryOperationPrototype>())
        {
            foreach (var step in operation.Steps)
            {
                if (!_proto.HasIndex<SurgeryStepPrototype>(step.ID))
                {
                    Logger.WarningS("surgery",
                        $"Invalid {nameof(SurgeryStepPrototype)} found in surgery operation with id {operation.ID}: No step found with id {step.ID}");
                }
            }
        }
    }

    /// <summary>
    /// Open the organ selection window for a player
    /// </summary>
    /// <returns>true if it was opened, false otherwise</return>
    public virtual bool SelectOrgan(EntityUid surgeon, EntityUid target)
    {
        return false;
    }
}
