using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface;

/// <summary>
/// Interface for <see cref="BoundUserInterface"/>s that need some updating logic
/// ran in the <see cref="ModUpdateLevel.PreEngine"/> stage.
/// </summary>
/// <remarks>
/// <para>
/// This is called on all open <see cref="BoundUserInterface"/>s that implement this interface.
/// </para>
/// <para>
/// One intended use case is coalescing input events (e.g. via <see cref="InputCoalescer{T}"/>) to send them to the
/// server only once per tick.
/// </para>
/// </remarks>
/// <seealso cref="BuiPreTickUpdateSystem"/>
public interface IBuiPreTickUpdate
{
    void PreTickUpdate();
}

/// <summary>
/// Implements <see cref="BuiPreTickUpdateSystem"/>.
/// </summary>
public sealed class BuiPreTickUpdateSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;

    private EntityQuery<UserInterfaceUserComponent> _userQuery;

    public override void Initialize()
    {
        base.Initialize();

        _userQuery = GetEntityQuery<UserInterfaceUserComponent>();
    }

    public void RunUpdates()
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var localSession = _playerManager.LocalSession;
        if (localSession?.AttachedEntity is not { } localEntity)
            return;

        if (!_userQuery.TryGetComponent(localEntity, out var userUIComp))
            return;

        foreach (var (entity, uis) in userUIComp.OpenInterfaces)
        {
            foreach (var key in uis)
            {
                if (!_uiSystem.TryGetOpenUi(entity, key, out var ui))
                {
                    DebugTools.Assert("Unable to find UI that was in the open UIs list??");
                    continue;
                }

                if (ui is IBuiPreTickUpdate tickUpdate)
                {
                    tickUpdate.PreTickUpdate();
                }
            }
        }
    }
}
