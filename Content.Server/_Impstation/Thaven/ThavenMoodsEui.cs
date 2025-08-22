using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared._Impstation.Thaven;
using Content.Shared._Impstation.Thaven.Components;

namespace Content.Server._Impstation.Thaven;

public sealed class ThavenMoodsEui : BaseEui
{
    private readonly ThavenMoodsSystem _thavenMoodsSystem;
    private readonly EntityManager _entityManager;
    private readonly IAdminManager _adminManager;

    private List<ThavenMood> _moods = new();
    private List<ThavenMood> _sharedMoods = new();
    private ISawmill _sawmill = default!;
    private EntityUid _target;

    public ThavenMoodsEui(ThavenMoodsSystem thavenMoodsSystem, EntityManager entityManager, IAdminManager manager)
    {
        _thavenMoodsSystem = thavenMoodsSystem;
        _entityManager = entityManager;
        _adminManager = manager;
        _sawmill = Logger.GetSawmill("thaven-moods-eui");
    }

    public override EuiStateBase GetNewState()
    {
        return new ThavenMoodsEuiState(_moods, _entityManager.GetNetEntity(_target));
    }

    public void UpdateMoods(ThavenMoodsComponent? comp, EntityUid player)
    {
        if (!IsAllowed())
            return;

        var moods = _thavenMoodsSystem.GetActiveMoods(player, comp, false);
        _target = player;
        _moods = moods;
        _sharedMoods = _thavenMoodsSystem.SharedMoods.ToList();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ThavenMoodsSaveMessage message)
            return;

        if (!IsAllowed())
            return;

        var player = _entityManager.GetEntity(message.Target);

        _thavenMoodsSystem.SetMoods(player, message.Moods);
    }

    private bool IsAllowed()
    {
        var adminData = _adminManager.GetAdminData(Player);
        if (adminData == null || !adminData.HasFlag(AdminFlags.Moderator))
        {
            _sawmill.Warning($"Player {Player.UserId} tried to open / use thaven moods UI without permission.");
            return false;
        }

        return true;
    }
}
