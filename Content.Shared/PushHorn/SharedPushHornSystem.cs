using Content.Shared.Interaction.Events;

namespace Content.Shared.PushHorn;

public abstract class SharedPushHornSystem : EntitySystem
{
    public virtual void UseInHand(Entity<PushHornComponent> ent, ref UseInHandEvent args)
    {
    }
}
