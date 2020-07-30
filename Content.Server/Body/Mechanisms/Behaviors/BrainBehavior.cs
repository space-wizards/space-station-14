using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Body.Mechanisms.Behaviors
{
    /// <summary>
    ///     The behaviors of a brain, inhabitable by a player.
    /// </summary>
    public class BrainBehavior : MechanismBehavior
    {
        public BrainBehavior(Mechanism parent) : base(parent) { }

        public override void Initialize()
        {
        }

        public override void OnInstallIntoBodyPartManager(IEntity attachedEntity)
        {
        }

        public override void OnInstallIntoBodyPart(IEntity attachedEntity)
        {
        }

        public override void OnRemoveFromBodyPartManager(IEntity attachedEntity)
        {
        }

        public override void OnRemoveFromBodyPart(IEntity attachedEntity)
        {
        }

        public override void Tick(float frameTime)
        {
        }
    }
}
