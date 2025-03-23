using Content.Shared.Interaction.Events;
using Content.Shared.PushHorn;
using Content.Shared.RepulseAttract;

namespace Content.Server.PushHorn
{
    internal sealed class PushHornSystem : SharedPushHornSystem
    {
        [Dependency] private readonly RepulseAttractSystem _repulse = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<PushHornComponent, UseInHandEvent>(UseInHand);
        }

        public override void UseInHand(Entity<PushHornComponent> ent, ref UseInHandEvent args)
        {
            if (TryComp<RepulseAttractComponent>(ent, out var repulseAttract))
            {
                var position = _transform.GetMapCoordinates(args.User);
                Log.Debug($"{repulseAttract.Speed}");
                _repulse.TryRepulseAttract(position, args.User, repulseAttract.Speed, repulseAttract.Range, repulseAttract.Whitelist, repulseAttract.CollisionMask);
            }
        }
    }
}

