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
using Robust.Shared.Prototypes;
using Content.Shared.Dataset;

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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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
            var system = _entityManager.System<PsychosisSystem>();
            _entityManager.System<PaperSystem>().TryStamp(uid, new StampDisplayInfo { StampedName = "stamp-component-stamped-name-centcom", StampedColor = Color.FromHex("#006600") }, "paper_stamp-centcom", papcomp);
            _entityManager.System<PaperSystem>().UpdateUserInterface(uid, papcomp);
            var psychosisMessage = Loc.GetString("psychosis-heal-psychosis", ("heal1psychosis-popup-headache", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-headache", 1))), ("heal2psychosis-popup-headache", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-headache", 2))), ("heal3psychosis-popup-headache", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-headache", 3))), ("heal1psychosis-popup-alert", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-alert", 1))), ("heal2psychosis-popup-alert", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-alert", 2))), ("heal3psychosis-popup-alert", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-alert", 3))), ("heal1psychosis-popup-eyes", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-eyes", 1))), ("heal2psychosis-popup-eyes", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-eyes", 2))), ("heal3psychosis-popup-eyes", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-eyes", 3))), ("heal1psychosis-popup-headrotate", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-headrotate", 1))), ("heal2psychosis-popup-headrotate", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-headrotate", 2))), ("heal3psychosis-popup-headrotate", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-headrotate", 3))), ("heal1psychosis-popup-skinfeel", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-skinfeel", 1))), ("heal2psychosis-popup-skinfeel", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-skinfeel", 2))), ("heal3psychosis-popup-skinfeel", Loc.GetString("psychosis-" + system.GetHeal("psychosis-popup-skinfeel", 3))));
            _paper.SetContent(uid, psychosisMessage.ToString());
        }
    }
}
