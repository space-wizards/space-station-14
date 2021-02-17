using Content.Server.Administration;
using Content.Server.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;

namespace Content.Server
{
    [UsedImplicitly]
    public sealed class SetOutfitEui : BaseEui
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        private readonly IEntity _target;
        public SetOutfitEui(IEntity entity)
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
                TargetEntityId = _target.Uid
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
