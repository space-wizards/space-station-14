using System;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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

