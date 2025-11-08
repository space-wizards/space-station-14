using Content.Shared.Polymorph;
using Robust.Shared.GameObjects;
using Content.Server.Polymorph.Systems;

namespace Content.Server.Polymorph
{
    /// <summary>
    /// Handles polymorph actions on items marked as single-use. When such an item is activated,
    /// the performer will be polymorphed and the item will be deleted.
    /// </summary>
    public sealed class DeleteOnUseSystem : EntitySystem
    {
        [Dependency] private readonly PolymorphSystem _polymorph = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DeleteOnUseComponent, PolymorphActionEvent>(OnPolymorphActionAtItem);
        }

        private void OnPolymorphActionAtItem(EntityUid uid, DeleteOnUseComponent comp, ref PolymorphActionEvent args)
        {
            if (args.Handled || args.ProtoId == null)
                return;

            var performer = args.Performer;
            if (Deleted(performer))
                return;

            // Try to polymorph the performer using the configured proto.
            var result = _polymorph.PolymorphEntity(performer, args.ProtoId.Value);
            if (result != null)
            {
                // Successful polymorph -> delete the item to make it single-use.
                QueueDel(uid);
                args.Handled = true;
            }
        }
    }
}
