using Content.Server.GhostKick;
using Content.Server.Popups;
using Content.Server.Silicons.Laws;
using Content.Shared.EntityEffects;

namespace Content.Server.Administration.Verbs.Operations;

/// <summary>
/// Executes data-defined admin operations by raising their local events on the target.
/// </summary>
public sealed partial class AdminOperationSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private GhostKickManager _ghostKick = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SiliconLawSystem _siliconLaws = default!;

    /// <summary>
    /// Raises the strongly typed local event used by an operation's handler.
    /// </summary>
    public void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>
    {
        var operationEvent = new AdminOperationEvent<T>(operation, user);
        RaiseLocalEvent(target, ref operationEvent);
    }

    /// <summary>
    /// Executes every operation synchronously in list order.
    /// </summary>
    public void Execute(EntityUid target, EntityUid user, IReadOnlyList<AdminOperation> operations)
    {
        foreach (var operation in operations)
        {
            operation.RaiseEvent(target, user, this);
        }
    }
}
