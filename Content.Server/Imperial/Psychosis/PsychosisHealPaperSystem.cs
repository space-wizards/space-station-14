using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Server.Paper;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Traits.Assorted;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;

namespace Content.Server.Psychosis
{
    public sealed class PsychosisHealPaperSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsychosisHealPaperComponent, MapInitEvent>(OnMapInit,
                after: new[] { typeof(PsychosisSystem) });
        }

        private void OnMapInit(EntityUid uid, PsychosisHealPaperComponent component, MapInitEvent args)
        {
            SetupPaper(uid, component);
        }

        private void SetupPaper(EntityUid uid, PsychosisHealPaperComponent? component = null, EntityUid? station = null)
        {
            if (!Resolve(uid, ref component))
                return;
            if (!TryComp<PaperComponent>(uid, out var papcomp))
                return;
            _entityManager.System<PaperSystem>().TryStamp(uid, new StampDisplayInfo { StampedName = "stamp-component-stamped-name-centcom", StampedColor = Color.FromHex("#006600") }, "paper_stamp-centcom", papcomp);
            _entityManager.System<PaperSystem>().UpdateUserInterface(uid, papcomp);
            var psychosisMessage = new FormattedMessage();
            for (var str = 0; str < 29; str++)
            {
                if (str == 0)
                    continue;
                if (str == 13)
                {
                    psychosisMessage.AddMarkup(Loc.GetString("heal-psychosis-13", ("first", Loc.GetString(_entityManager.System<PsychosisSystem>().GetFirst()))));
                    psychosisMessage.PushNewline();
                    continue;
                }
                if (str == 17)
                {
                    psychosisMessage.AddMarkup(Loc.GetString("heal-psychosis-17", ("second", Loc.GetString(_entityManager.System<PsychosisSystem>().GetSecond()))));
                    psychosisMessage.PushNewline();
                    continue;
                }
                if (str == 21)
                {
                    psychosisMessage.AddMarkup(Loc.GetString("heal-psychosis-21", ("third", Loc.GetString(_entityManager.System<PsychosisSystem>().GetThird()))));
                    psychosisMessage.PushNewline();
                    continue;
                }
                if (Loc.GetString("heal-psychosis-" + str.ToString()) == "empty")
                {
                    psychosisMessage.PushNewline();
                    continue;
                }
                psychosisMessage.AddMarkup(Loc.GetString("heal-psychosis-" + str.ToString()));
                if (str != 28)
                    psychosisMessage.PushNewline();
            }
            //psychosisMessage.AddMarkup(Loc.GetString("heal-psychosis-listfirst", ("first", Loc.GetString(_entityManager.System<PsychosisSystem>().GetFirst()))));
            _paper.SetContent(uid, psychosisMessage.ToString());
        }
    }
}
