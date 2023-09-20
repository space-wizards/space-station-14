using Robust.Shared.Containers;

namespace Content.Server.Containers
{
    public sealed class ShredderSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShredderComponent, EntInsertedIntoContainerMessage>(OnInsert);
        }
        private void OnInsert(EntityUid uid, ShredderComponent comp, EntInsertedIntoContainerMessage args)
        {
            if (!comp.Whitelist.IsValid(args.Entity, EntityManager))
                return;

            var ev = new BeingShreddedEvent(args.Entity);
            RaiseLocalEvent(args.Entity, ev);
            if (ev.Handled)
            {
                _audio.PlayPvs(comp.ShreddingSound, uid);
            }
        }
    }
}
