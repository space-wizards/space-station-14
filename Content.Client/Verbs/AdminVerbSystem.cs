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
            if (_clientConGroupController.CanViewVar())
            {
                Verb verb = new("ViewVariables");
                verb.Category = VerbCategory.Debug;
                verb.Text = "View Variables";
                verb.IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png";
                verb.Act = () => _viewVariablesManager.OpenVV(args.Target);
                args.Verbs.Add(verb);
            }

            // Then we have some atmos admin verbs that target grid tiles.
            // We do not have verb click coords, so we just check if there is an applicable grid near the target entity

            // TODO VERBS support verbs when right clicking empty tiles? E.g., can point at a tile via keybind but
            // not via verbs. Would require assemble verbs event to support a null-able entity and pass map coords?
            // Then make this verb hidden unless the user is acting on a tile?

            var coords = args.Target.Transform.MapPosition;
            if (!_mapManager.TryFindGridAt(coords, out var grid))
                return;
            var tileIndices = grid.WorldToTile(coords.Position);

            // Verb to open Add Gas admin window, with pre-filled coordinates
            if (_clientConGroupController.CanCommand("addgas"))
            {
                Verb verb = new("addgas");
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
                Verb verb = new("settemp");
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
