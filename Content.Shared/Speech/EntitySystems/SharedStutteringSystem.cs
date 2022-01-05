using System;
using Content.Shared.Alert;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;

namespace Content.Shared.Speech.EntitySystems
{
    public abstract class SharedStutteringSystem : EntitySystem
    {
        // For code in shared... I imagine we ain't getting accent prediction anytime soon so let's not bother.
        public virtual void DoStutter(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null, SharedAlertsComponent? alerts = null)
        {
        }
    }
}
