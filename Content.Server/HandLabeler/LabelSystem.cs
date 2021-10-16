using Content.Server.HandLabeler.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.HandLabeler
{
    /// <summary>
    /// A system that lets players see the contents of a label on an object.
    /// </summary>
    [UsedImplicitly]
    public class LabelSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LabelComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, LabelComponent? label, ExaminedEvent args)
        {
            if (!Resolve(uid, ref label))
                return;

            if (label.CurrentLabel == null)
                return;

            var message = new FormattedMessage();
            message.AddText(Loc.GetString("hand-labeler-has-label", ("label", label.CurrentLabel)));
            args.PushMessage(message);
        }
    }
}
