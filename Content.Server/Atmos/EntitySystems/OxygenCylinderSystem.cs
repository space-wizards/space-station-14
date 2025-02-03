using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class OxygenCylinderSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Update(float frameTime)
        {
            foreach (var (oxygen, transform) in EntityManager.EntityQuery<OxygenCylinderComponent, TransformComponent>())
            {
                oxygen.CurrentOxygen -= oxygen.OxygenUseRate * frameTime;
                oxygen.CurrentOxygen = Math.Max(0, oxygen.CurrentOxygen);

                if (_playerManager.TryGetSessionByEntity(transform.Owner, out var session))
                {
                    UpdateOxygenUI(session, oxygen);
                }
            }
        }

        private void UpdateOxygenUI(IPlayerSession session, OxygenCylinderComponent oxygen)
        {
            if (session.AttachedEntity == null)
                return;

            var oxygenBarControl = IoCManager.Resolve<IUserInterfaceManager>().GetUIController<OxygenBarControl>();
            oxygenBarControl.UpdateOxygenLevel(oxygen.CurrentOxygen, oxygen.MaxOxygen);
        }
    }
}
