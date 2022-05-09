using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.FlavorText
{
    public sealed class FlavorTextSystem : EntitySystem
    {
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FlavorTextComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        }

        private void OnGetExamineVerbs(EntityUid uid, FlavorTextComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);

            var verb = new ExamineVerb()
            {
                Act = () =>
                {
                    var markup = new FormattedMessage();
                    markup.AddMarkup(component.Content);
                    _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
                },
                Text = Loc.GetString("flavortext-examinable-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = Loc.GetString("flavortext-examinable-verb-disabled"),
                IconTexture = "/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"
            };

            args.Verbs.Add(verb);
        }
    }
}
