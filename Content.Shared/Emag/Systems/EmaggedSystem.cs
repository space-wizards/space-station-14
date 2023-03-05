using Content.Shared.Emag.Systems;
using Content.Shared.Emag.Components;

namespace Content.Shared.Emag.Systems
{
    public sealed class EmaggedSystem : EntitySystem
    {
        public override void Initialize ()
        {
            base.Initialize();
            SubscribeLocalEvent<BeEmaggedComponent, GotEmaggedEvent>(OnEmagged);
        }
        private void OnEmagged(EntityUid uid, BeEmaggedComponent reader, ref GotEmaggedEvent args)
        {
            if (reader.Enabled)
            {
                reader.Enabled = false;
                args.Handled = true;
            }
        }

    }
}
