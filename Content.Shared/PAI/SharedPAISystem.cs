using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;

namespace Content.Shared.PAI
{
    /// <summary>
    /// pAIs, or Personal AIs, are essentially portable ghost role generators.
    /// In their current implementation, they create a ghost role anyone can access,
    /// and that a player can also "wipe" (reset/kick out player).
    /// Theoretically speaking pAIs are supposed to use a dedicated "offer and select" system,
    ///  with the player holding the pAI being able to choose one of the ghosts in the round.
    /// This seems too complicated for an initial implementation, though,
    ///  and there's not always enough players and ghost roles to justify it.
    /// </summary>
    public abstract class SharedPAISystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PAIComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PAIComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnStartup(EntityUid uid, PAIComponent component, ComponentStartup args)
        {
            if (component.MidiAction != null)
                _actionsSystem.AddAction(uid, component.MidiAction, null);
        }

        private void OnShutdown(EntityUid uid, PAIComponent component, ComponentShutdown args)
        {
            if (component.MidiAction != null)
                _actionsSystem.RemoveAction(uid, component.MidiAction);
        }
    }
}

