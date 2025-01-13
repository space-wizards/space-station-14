using Content.Shared.Actions;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// Manages Borg disguises, such as the Syndicate Saboteur's chameleon projector.
/// </summary>
public abstract class SharedBorgDisguiseSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgDisguiseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgDisguiseComponent, ComponentShutdown>(OnCompRemove);
    }

    /// <summary>
    /// Swaps the description of the entity based on the disguise state.
    /// </summary>
    /// <param name="uid">The entity to swap</param>
    /// <param name="comp">The component to use for getting the disguise state and description.</param>
    protected void SwapDescription(EntityUid uid, BorgDisguiseComponent comp)
    {
        if (comp.Disguised)
        {
            _meta.SetEntityDescription(uid, comp.Description);
        }
        else
        {
            TryPrototype(uid, out var metaData);
            if (metaData is not null)
                _meta.SetEntityDescription(uid, metaData.Description);
        }
    }

    #region ActionManagement

    /// <summary>
    /// Gives the action to disguise
    /// </summary>
    private void OnMapInit(EntityUid uid, BorgDisguiseComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    /// <summary>
    /// Takes away the action to disguise from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, BorgDisguiseComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }

    #endregion
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class BorgDisguiseToggleActionEvent : InstantActionEvent
{
}
