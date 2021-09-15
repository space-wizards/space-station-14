using Content.Client.Administration.UI.Tabs.AtmosTab;
using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Verbs
{
    /// <summary>
    ///     Client-side admin verb system. These usually open some sort of UIs.
    /// </summary>
    class AdminVerbSystem : EntitySystem
    {
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;
        [Dependency] private readonly IViewVariablesManager _viewVariablesManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<GetOtherVerbsEvent>(AddAdminVerbs);
        }

        private void AddAdminVerbs(GetOtherVerbsEvent args)
        {
            // View variables verbs
            if (_clientConGroupController.CanViewVar())
            {
                Verb verb = new();
                verb.Category = VerbCategory.Debug;
                verb.Text = "View Variables";
                verb.IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png";
                verb.Act = () => _viewVariablesManager.OpenVV(args.Target);
                args.Verbs.Add(verb);
            }

            // Then add some atmos admin verbs that target grid tiles.
            // However, we do not have verb click coords, and the context menu does not target tiles.
            // So instead, try find a tile at the clicked entities coordinates:
            var coords = args.Target.Transform.MapPosition;
            if (!_mapManager.TryFindGridAt(coords, out var grid))
                return;
            var tileIndices = grid.WorldToTile(coords.Position);

            // Verb to open Add Gas admin window, with pre-filled coordinates
            if (_clientConGroupController.CanCommand("addgas"))
            {
                Verb verb = new();
                verb.Category = VerbCategory.Debug;
                verb.Text = "Add Gas";
                // TODO VERB ICON Add gas symbol. A gas tank maybe?
                verb.Act = () =>
                {
                    var window = _dynamicTypeFactory.CreateInstance<AddGasWindow>();
                    window.FillCoords(grid.Index, tileIndices.X, tileIndices.Y);
                    window.OpenCentered();
                };
                args.Verbs.Add(verb);
            }

            // Verb to open Set temperature admin window, with pre-filled coordinates
            if (_clientConGroupController.CanCommand("settemp"))
            {
                Verb verb = new();
                verb.Category = VerbCategory.Debug;
                verb.Text = "Set Temperature";
                // TODO VERB ICON add thermometer?
                verb.Act = () =>
                {
                    var window = _dynamicTypeFactory.CreateInstance<SetTemperatureWindow>();
                    window.FillCoords(grid.Index, tileIndices.X, tileIndices.Y);
                    window.OpenCentered();
                };
                args.Verbs.Add(verb);
            }
        }
    }
}
