using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.Silicons.Laws;

public sealed class SiliconLawEui : BaseEui
{
    private readonly SiliconLawSystem _siliconLawSystem;
    private readonly EntityManager _entityManager;
    private readonly IAdminManager _adminManager;

    private List<SiliconLaw> _laws = new();
    private ISawmill _sawmill = default!;
    private EntityUid _target;

    public SiliconLawEui(SiliconLawSystem siliconLawSystem, EntityManager entityManager, IAdminManager manager)
    {
        _siliconLawSystem = siliconLawSystem;
        _adminManager = manager;
        _entityManager = entityManager;
        _sawmill = Logger.GetSawmill("silicon-law-eui");
    }

    public override EuiStateBase GetNewState()
    {
        return new SiliconLawsEuiState(_laws, _entityManager.GetNetEntity(_target));
    }

    public void UpdateLaws(SiliconLawBoundComponent? lawBoundComponent, EntityUid player)
    {
        if (!IsAllowed())
            return;

        var laws = _siliconLawSystem.GetLaws(player, lawBoundComponent);
        _laws = laws.Laws;
        _target = player;
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not SiliconLawsSaveMessage message)
        {
            return;
        }

        if (!IsAllowed())
            return;

        var player = _entityManager.GetEntity(message.Target);

        _siliconLawSystem.SetLaws(message.Laws, player);
    }

    private bool IsAllowed()
    {
        var adminData = _adminManager.GetAdminData(Player);
        if (adminData == null || !adminData.HasFlag(AdminFlags.Moderator))
        {
            _sawmill.Warning("Player {0} tried to open / use silicon law UI without permission.", Player.UserId);
            return false;
        }

        return true;
    }
}
