using System;
using Content.Shared.DragDrop;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

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
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PAIComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<PAIComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<PAIComponent, AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<PAIComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<PAIComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<PAIComponent, MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<PAIComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
        }

        private void OnMoveAttempt(EntityUid uid, PAIComponent component, MovementAttemptEvent args)
        {
            args.Cancel(); // no more scurrying around on lil robot legs.
        }

        private void OnChangeDirectionAttempt(EntityUid uid, PAIComponent component, ChangeDirectionAttemptEvent args)
        {
            // PAIs can't rotate, but decapitated heads and sentient crowbars can, life isn't fair. Seriously though, why
            // tf does this have to be actively blocked, surely this should just not be blanket enabled for any player
            // controlled entity. Same goes for moving really.
            args.Cancel();
        }

        private void OnUseAttempt(EntityUid uid, PAIComponent component, UseAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, PAIComponent component, InteractionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnAttackAttempt(EntityUid uid, PAIComponent component, AttackAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, PAIComponent component, DropAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, PAIComponent component, PickupAttemptEvent args)
        {
            args.Cancel();
        }
    }

    [Serializable, NetSerializable]
    public enum PAIVisuals : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum PAIStatus : byte
    {
        Off,
        Searching,
        On
    }
}

