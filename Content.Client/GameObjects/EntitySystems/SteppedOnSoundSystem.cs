using Content.Shared.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Client.GameObjects.EntitySystems
{
    internal sealed class SteppedOnSoundSystem : SharedSteppedOnSoundSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        protected override bool CanPlaySound(EntityUid uid)
        {
            if (!base.CanPlaySound(uid)) return false;

            return _playerManager.LocalPlayer?.ControlledEntity?.Uid == uid;
        }

        protected override Filter GetFilter(IEntity entity)
        {
            return Filter.Local();
        }
    }
}
