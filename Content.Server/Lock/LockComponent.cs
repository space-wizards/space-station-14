using Content.Server.Lock;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    /// <summary>
    /// Allows locking/unlocking, with access determined by AccessReader
    /// </summary>
    [RegisterComponent]
    public class LockComponent : Component
    {
        public override string Name => "Lock";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("locked")] public bool Locked { get; set; } = true;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("unlockingSound")] public SoundSpecifier? UnlockSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");
        [ViewVariables(VVAccess.ReadWrite)] [DataField("lockingSound")] public SoundSpecifier? LockSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");

        [Verb]
        private sealed class ToggleLockVerb : Verb<LockComponent>
        {
            protected override void GetData(IEntity user, LockComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) ||
                    component.Owner.TryGetComponent(out EntityStorageComponent? entityStorageComponent) && entityStorageComponent.Open)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock");
            }

            protected override void Activate(IEntity user, LockComponent component)
            {
                // Do checks
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) ||
                    !user.InRangeUnobstructed(component))
                {
                    return;
                }

                // Call relevant entity system
                var lockSystem = user.EntityManager.EntitySysManager.GetEntitySystem<LockSystem>();
                var eventData = new ActivateInWorldEvent(user, component.Owner);
                if (component.Locked)
                {
                    lockSystem.DoUnlock(component, eventData);
                }
                else
                {
                    lockSystem.DoLock(component, eventData);
                }
            }
        }
    }
}
