using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared._Impstation.Spelfs;
using Content.Shared._Impstation.Spelfs.Components;

namespace Content.Server._Impstation.Spelfs;

public sealed class SpelfMoodsEui : BaseEui
{
    private readonly SpelfMoodsSystem _spelfMoodsSystem;
    private readonly EntityManager _entityManager;
    private readonly IAdminManager _adminManager;

    private List<SpelfMood> _moods = new();
    private List<SpelfMood> _sharedMoods = new();
    private ISawmill _sawmill = default!;
    private EntityUid _target;

    public SpelfMoodsEui(SpelfMoodsSystem spelfMoodsSystem, EntityManager entityManager, IAdminManager manager)
    {
        _spelfMoodsSystem = spelfMoodsSystem;
        _entityManager = entityManager;
        _adminManager = manager;
        _sawmill = Logger.GetSawmill("spelf-moods-eui");
    }

    public override EuiStateBase GetNewState()
    {
        return new SpelfMoodsEuiState(_moods, _entityManager.GetNetEntity(_target));
    }

    public void UpdateMoods(SpelfMoodsComponent? comp, EntityUid player)
    {
        if (!IsAllowed())
            return;

        var moods = _spelfMoodsSystem.GetActiveMoods(player, comp, false);
        _target = player;
        _moods = moods;
        _sharedMoods = _spelfMoodsSystem.SharedMoods.ToList();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SpelfMoodsSaveMessage message)
            return;

        if (!IsAllowed())
            return;

        var player = _entityManager.GetEntity(message.Target);

        _spelfMoodsSystem.SetMoods(player, message.Moods);
    }

    private bool IsAllowed()
    {
        var adminData = _adminManager.GetAdminData(Player);
        if (adminData == null || !adminData.HasFlag(AdminFlags.Moderator))
        {
            _sawmill.Warning($"Player {Player.UserId} tried to open / use spelf moods UI without permission.");
            return false;
        }

        return true;
    }
}
