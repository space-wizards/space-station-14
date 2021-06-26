using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
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
        [ViewVariables(VVAccess.ReadWrite)] [DataField("unlockingSound")] public string UnlockSound { get; set; } = "/Audio/Machines/door_lock_off.ogg";
        [ViewVariables(VVAccess.ReadWrite)] [DataField("lockingSound")] public string LockSound { get; set; } = "/Audio/Machines/door_lock_off.ogg";
        [ViewVariables(VVAccess.ReadWrite)] public LockComponentStateVerbOverride VerbOverride { get; set; }

        // TODO to be removed once Verbs are refactored
        [Verb]
        private sealed class ToggleLockVerb : Verb<LockComponent>
        {
            protected override void GetData(IEntity user, LockComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) ||
                    (component.Owner.TryGetComponent(out EntityStorageComponent? entityStorageComponent) && entityStorageComponent.Open))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock");
            }

            protected override void Activate(IEntity user, LockComponent component)
            {
                component.VerbOverride = component.Locked ? LockComponentStateVerbOverride.DoUnlock
                                                          : LockComponentStateVerbOverride.DoLock;

                // HACK to force an action from the LockSystem. Will most probably be removed during Verb system refactor
                user.EntityManager.EventBus.RaiseLocalEvent(component.Owner.Uid, new ActivateInWorldEvent(user, component.Owner));
            }
        }
    }

    /// <summary>
    /// If other than None, then the LockSystem will attempt to do the specifiec action on next pass
    /// </summary>
    public enum LockComponentStateVerbOverride
    {
        None,
        DoLock,
        DoUnlock
    }
}
