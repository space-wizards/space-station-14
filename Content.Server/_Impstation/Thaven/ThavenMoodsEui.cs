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
    private readonly ThavenMoodsSystem _moodsSystem;
    private readonly EntityManager _entMan;
    private readonly IAdminManager _adminManager;

    private List<ThavenMood> _moods = new();
    private List<ThavenMood> _sharedMoods = new();
    private ISawmill _sawmill = default!;
    private EntityUid _target;

    public ThavenMoodsEui(ThavenMoodsSystem thavenMoodsSystem, EntityManager entityManager, IAdminManager manager)
    {
        _moodsSystem = thavenMoodsSystem;
        _entMan = entityManager;
        _adminManager = manager;
        _sawmill = Logger.GetSawmill("thaven-moods-eui");
    }

    public override EuiStateBase GetNewState()
    {
        return new ThavenMoodsEuiState(_moods, _entMan.GetNetEntity(_target));
    }

    public void UpdateMoods(Entity<ThavenMoodsComponent> ent)
    {
        if (!IsAllowed())
            return;

        _target = ent;
        _moods = ent.Comp.Moods;
        _sharedMoods = _moodsSystem.SharedMoods.ToList();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ThavenMoodsSaveMessage message)
            return;

        if (!IsAllowed())
            return;

        var uid = _entMan.GetEntity(message.Target);
        if (!_entMan.TryGetComponent<ThavenMoodsComponent>(uid, out var comp))
        {
            _sawmill.Warning($"Entity {_entMan.ToPrettyString(uid)} does not have ThavenMoodsComponent!");
            return;
        }

        _moodsSystem.SetMoods((uid, comp), message.Moods);
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
