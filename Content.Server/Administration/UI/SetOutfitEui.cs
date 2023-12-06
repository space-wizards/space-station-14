using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server.Administration.UI
{
    [UsedImplicitly]
    public sealed class SetOutfitEui : BaseEui
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        private readonly EntityUid _target;

        public SetOutfitEui(EntityUid entity)
        {
            _target = entity;
            IoCManager.InjectDependencies(this);
        }

        public override void Opened()
        {
            base.Opened();

            StateDirty();
            _adminManager.OnPermsChanged += AdminManagerOnPermsChanged;
        }

        public override EuiStateBase GetNewState()
        {
            return new SetOutfitEuiState
            {
                TargetNetEntity = _entManager.GetNetEntity(_target)
            };
        }

        private void AdminManagerOnPermsChanged(AdminPermsChangedEventArgs obj)
        {
            // Close UI if user loses +FUN.
            if (obj.Player == Player && !UserAdminFlagCheck(AdminFlags.Fun))
            {
                Close();
            }
        }
        private bool UserAdminFlagCheck(AdminFlags flags)
        {
            return _adminManager.HasAdminFlag(Player, flags);
        }

    }
}
