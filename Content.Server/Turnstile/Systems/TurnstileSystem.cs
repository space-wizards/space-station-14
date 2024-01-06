using Content.Shared.Physics;
using Content.Shared.Turnstile.Components;
using Content.Shared.Turnstile.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;

namespace Content.Server.Turnstile.Systems;

public sealed class TurnstileSystem : SharedTurnstileSystem
{
    protected override void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {
        // If the colliding entity cannot open doors by bumping into them, then it can't turn the turnstile either.
        if (!Tags.HasTag(args.OtherEntity, "DoorBumpOpener"))
            return;

        // Check the contact normal against our direction.
        // For simplicity, we always want a mob to pass from the "back" to the "front" of the turnstile.
        // That allows unanchored turnstiles to be dragged and rotated as needed, and will admit passage in the
        // direction that they are pulled in.
        var turnstile = ent.Comp;
        if (turnstile.State == TurnstileState.Idle)
        {
            var facingDirection = GetFacingDirection(ent.Owner, turnstile);
            var directionOfContact = GetDirectionOfContact(ent.Owner, args.OtherEntity);
            if (facingDirection == directionOfContact)
            {
                // Admit the entity.
                SetState(ent.Owner, TurnstileState.Rotating);

                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.OtherEntity;

                // Play sound of turning
                Audio.PlayPvs(turnstile.TurnSound, ent.Owner, AudioParams.Default.WithVolume(-3));

                // Break the physics contact so that they can now pass through.
                PhysicsSystem.DestroyContacts(args.OurBody);

            }
            else
            {
                // Reject the entity, play sound with cooldown
                Audio.PlayPvs(turnstile.BumpSound, ent.Owner, AudioParams.Default.WithVolume(-3));
            }
        }
        else
        {
            // Reject the entity, play sound with cooldown
            Audio.PlayPvs(turnstile.BumpSound, ent.Owner, AudioParams.Default.WithVolume(-3));
        }
    }
}
