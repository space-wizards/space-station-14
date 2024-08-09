using System.Numerics;
using System.Text;
using Content.Client.Administration.Managers;
using Content.Shared.Administration.Managers;
using Content.Shared.NPC.NuPC;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Client.NPC.NuPC;


public sealed class NpcGoalSystem : SharedNpcGoalSystem
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IConsoleHost _host = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    private NpcGoalsDebugEvent? _ev;

    private bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value || (value && !_admin.IsAdmin()))
                return;

            _enabled = value;
            RaiseNetworkEvent(new RequestNpcGoalsEvent()
            {
                Enabled = _enabled,
            });

            if (_enabled)
            {
                _overlays.AddOverlay(new NpcGoalOverlay(EntityManager, _cache));
            }
            else
            {
                _ev = null;
                _overlays.RemoveOverlay<NpcGoalOverlay>();
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NpcGoalsDebugEvent>(OnGoalsDebug);

        _host.RegisterCommand("npc_goals", Loc.GetString("cmd-npc_goals-desc"), Loc.GetString("cmd-npc_goals-help"), DebugCommand);
    }

    private void DebugCommand(IConsoleShell shell, string argstr, string[] args)
    {
        Enabled = !Enabled;
        shell.WriteLine(Loc.GetString("npc_debug-command", ("enabled", Enabled)));
    }

    private void OnGoalsDebug(NpcGoalsDebugEvent ev)
    {
        _ev = ev;
    }

    private sealed class NpcGoalOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        private EntityManager _entManager;
        private NpcGoalSystem _goals;
        private SharedTransformSystem _xforms;

        private Font _font;

        public NpcGoalOverlay(EntityManager entManager, IResourceCache cache)
        {
            _entManager = entManager;

            _goals = _entManager.System<NpcGoalSystem>();
            _xforms = _entManager.System<SharedTransformSystem>();

            _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 12);
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (_goals._ev == null)
                return false;

            return base.BeforeDraw(in args);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (args.ViewportControl == null)
                return;

            foreach (var data in _goals._ev!.Data)
            {
                if (!_entManager.TryGetEntity(data.Owner, out var uid))
                    continue;

                var mapcoords = _xforms.GetMapCoordinates(uid.Value);

                if (mapcoords.MapId != args.MapId)
                    continue;

                var screenPos = args.ViewportControl.WorldToScreen(mapcoords.Position);
                var text = new StringBuilder();
                var offset = Vector2.Zero;

                if (data.Goals.Count > 0)
                {
                    text.AppendLine("Goals:");

                    foreach (var goal in data.Goals)
                    {
                        var gText = $"- {goal.GetType().Name}";
                        text.AppendLine(gText);
                    }

                    var drawText = text.ToString().Trim();
                    offset = new Vector2(0f, args.ScreenHandle.GetDimensions(_font, drawText, 1f).Y);
                    args.ScreenHandle.DrawString(_font, screenPos, drawText, Color.Lime);
                }

                if (data.Generators.Count > 0)
                {
                    text.Clear();
                    text.AppendLine("Generators:");

                    foreach (var gen in data.Generators)
                    {
                        var gText = $"- {gen.GetType().Name}";
                        text.AppendLine(gText);
                    }

                    var drawText = text.ToString().Trim();
                    args.ScreenHandle.DrawString(_font, screenPos + offset, drawText, Color.Orange);
                }
            }
        }
    }
}
