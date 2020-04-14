using System;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon
{
    [RegisterComponent]
    public sealed class ServerFlashableComponent : SharedFlashableComponent
    {
        private double _duration;
        private TimeSpan _lastFlash;

        public void Flash(double duration)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            _lastFlash = timing.CurTime;
            _duration = duration;
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new FlashComponentState(_duration, _lastFlash);
        }
    }
}
