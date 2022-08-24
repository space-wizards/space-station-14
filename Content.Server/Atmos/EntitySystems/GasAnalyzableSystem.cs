using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasAnalyzableSystem : EntitySystem
    {
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasAnalyzableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
            SubscribeLocalEvent<GasAnalyzerComponent, BoundUIClosedEvent>((_,c,_) => c.UpdateAppearance(false));
        }

        private void OnGetExamineVerbs(EntityUid uid, GasAnalyzableComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            // Must be in details range to try this.
            if (_examineSystem.IsInDetailsRange(args.User, args.Target))
            {
                var held = args.Using;
                var enabled = held != null && EntityManager.HasComponent<SharedGasAnalyzerComponent>(held);
                var verb = new ExamineVerb
                {
                    Disabled = !enabled,
                    Message = Loc.GetString("gas-analyzable-system-verb-tooltip"),
                    Text = Loc.GetString("gas-analyzable-system-verb-name"),
                    Category = VerbCategory.Examine,
                    IconTexture = "/Textures/Interface/VerbIcons/examine.svg.192dpi.png",
                    Act = () =>
                    {
                        var markup = FormattedMessage.FromMarkup(GeneratePipeMarkup(uid));
                        _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
                    }
                };

                args.Verbs.Add(verb);
            }
        }

        private string GeneratePipeMarkup(EntityUid uid, NodeContainerComponent? nodeContainer = null)
        {
            if (!Resolve(uid, ref nodeContainer))
                return Loc.GetString("gas-analyzable-system-internal-error-missing-component");

            List<string> portNames = new List<string>();
            List<string> portData = new List<string>();
            foreach (var node in nodeContainer.Nodes)
            {
                if (node.Value is not PipeNode pn)
                    continue;
                float pressure = pn.Air.Pressure;
                float temp = pn.Air.Temperature;
                portNames.Add(node.Key);
                portData.Add(Loc.GetString("gas-analyzable-system-statistics",
                    ("pressure", pressure),
                    ("tempK", $"{temp:0.#}"),
                    ("tempC", $"{TemperatureHelpers.KelvinToCelsius(temp):0.#}")
                ));
            }

            int count = portNames.Count;
            if (count == 0)
                return Loc.GetString("gas-anlayzable-system-internal-error-no-gas-node");
            else if (count == 1)
                // omit names if only one node
                return Loc.GetString("gas-analyzable-system-header") + "\n" + portData[0];
            else
            {
                var outputs = portNames.Zip(portData, ((name, data) => name + ":\n" + data));
                return Loc.GetString("gas-analyzable-system-header") + "\n\n" + String.Join("\n\n", outputs);
            }
        }
    }
}
