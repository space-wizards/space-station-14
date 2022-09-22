using Content.Server.Atmos.Components;
using Content.Server.Examine;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Temperature.Systems
{
    public sealed class TemperatureProtectionSystem : EntitySystem
    {
        [Dependency] private readonly ExamineSystem _examineSystem = default!;


        public override void Initialize()
        {
            SubscribeLocalEvent<TemperatureProtectionComponent, ModifyChangedTemperatureEvent>(OnTemperatureChangeAttempt);
            SubscribeLocalEvent<TemperatureProtectionComponent, GetVerbsEvent<ExamineVerb>>(OnExamineVerb);
            SubscribeLocalEvent<TemperatureProtectionComponent, ExamineStatsEvent>(OnExamineStats);
        }

        public void OnExamineStats(EntityUid uid, TemperatureProtectionComponent component, ExamineStatsEvent args)
        {
            if (component.Coefficient == 0)
                return;

            var level = "";

            switch (component.Coefficient)
            {
                case >= 1f:
                    level = Loc.GetString("temperature-protection-level-0");
                    break;
                case < 1f and >= 0.5f:
                    level = Loc.GetString("temperature-protection-level-1");
                    break;
                case < 0.5f and >= 0.1f:
                    level = Loc.GetString("temperature-protection-level-2");
                    break;
                case < 0.1f and >= 0.01f:
                    level = Loc.GetString("temperature-protection-level-3");
                    break;
                case < 0.01f and >= 0.005f:
                    level = Loc.GetString("temperature-protection-level-4");
                    break;
                case < 0.005f and >= 0.001f:
                    level = Loc.GetString("temperature-protection-level-5");
                    break;
                case < 0.001f and > 0f:
                    level = Loc.GetString("temperature-protection-level-6");
                    break;
                case <= 0f:
                    level = Loc.GetString("temperature-protection-level-7");
                    break;
            }

            args.Markup.Add(Loc.GetString("temperature-protection-examine", ("level", level)));
        }

        public void OnExamineVerb(EntityUid uid, TemperatureProtectionComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (component.Coefficient == 1)
                return;

            _examineSystem.CreateExamineDetailsVerb("worn-stats", args, "/Textures/Interface/VerbIcons/dot.svg.192dpi.png");
        }

        private void OnTemperatureChangeAttempt(EntityUid uid, TemperatureProtectionComponent component, ModifyChangedTemperatureEvent args)
        {
            args.TemperatureDelta *= component.Coefficient;
        }
    }
}
