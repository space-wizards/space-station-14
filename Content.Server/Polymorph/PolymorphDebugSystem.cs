using Content.Shared.Polymorph;
using Robust.Shared.GameObjects;
using Content.Shared.Popups;
using Robust.Shared.Localization;

namespace Content.Server.Polymorph
{
    /// <summary>
    /// Small debug helper: shows a popup/log when a polymorph action is performed.
    /// Useful to confirm the action fired and which proto was requested.
    /// </summary>
    public sealed class PolymorphDebugSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            // Global subscription to the polymorph action event so we can debug when it's used.
            SubscribeLocalEvent<PolymorphActionEvent>(OnPolymorphAction);
        }

        private void OnPolymorphAction(PolymorphActionEvent ev)
        {
            // Event should have the performer filled by the actions system.
            var performer = ev.Performer;
            if (performer == default)
                return;

            var proto = ev.ProtoId?.ToString() ?? "(none)";

            // Popup the performer so it's visible during testing.
            _popup.PopupClient(Loc.GetString("polymorph-debug-popup", ("proto", proto)), performer, performer);

            // Also write to the server log for non-interactive debugging.
            Logger.DebugS("polymorph", $"PolymorphActionEvent fired by {performer}: proto={proto}");
        }
    }
}
