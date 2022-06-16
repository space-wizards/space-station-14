using Content.Server.Polymorph.Systems;

/// temporary btw
namespace Content.Server.Borgs
{
    public sealed class BorgSystem : EntitySystem
    {
        [Dependency] private readonly PolymorphableSystem _polymorphableSystem = default!;


        Queue<EntityUid> RemQueue = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var borg in RemQueue)
            {
                RemComp<BorgComponent>(borg);
            }
            RemQueue.Clear();
            foreach (var borg in EntityQuery<BorgComponent>())
            {
                borg.Accumulator += frameTime;
                if (borg.Accumulator < 3.5f)
                    continue;
                _polymorphableSystem.PolymorphEntity(borg.Owner, "Borg");
                RemQueue.Enqueue(borg.Owner);
            }
        }
    }
}
