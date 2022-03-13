using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Server.Tools;
using Content.Shared.Examine;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasAnalyzableSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasAnalyzableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        }

        private void OnGetExamineVerbs(EntityUid uid, GasAnalyzableComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            // Must be in details range to try this.
            if (_examineSystem.IsInDetailsRange(args.User, args.Target))
            {
                var held = args.Using;

                var enabled = held != null;
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

            foreach (var node in nodeContainer.Nodes)
            {
                if (!(node.Value is PipeNode))
                    continue;
                var pn = (PipeNode)node.Value;
                float pressure = pn.Air.Pressure;
                float temp = pn.Air.Temperature;
                return Loc.GetString("gas-analyzable-system-statistics",
                    ("pressure", pressure),
                    ("tempK", $"{temp:0.#}"),
                    ("tempC", $"{TemperatureHelpers.KelvinToCelsius(temp):0.#}")
                );
            }
            return Loc.GetString("gas-anlayzable-system-internal-error-no-gas-node");
        }
    }
}
