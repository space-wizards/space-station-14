using Content.Server.Paper;

namespace Content.Server.Nuke
{
    public sealed class NukeCodePaperSystem : EntitySystem
    {
        [Dependency] private readonly NukeCodeSystem _codes = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeCodePaperComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, NukeCodePaperComponent component, MapInitEvent args)
        {
            var msg = Loc.GetString("nuke-paper-content", ("code", _codes.Code));
            _paperSystem.SetContent(uid, msg);
        }
    }
}
