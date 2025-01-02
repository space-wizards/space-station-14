using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._Starlight.UI;
using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Controls;
using Content.Shared._Starlight.Computers.Recruitment;
using Content.Shared.Roles;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;
using YamlDotNet.Core.Tokens;

namespace Content.Client._Starlight.Computers.Recruitment;

[UsedImplicitly]
public sealed class RecruitmentComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly ILocalizationManager Loc = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;

    private readonly Dictionary<ProtoId<JobPrototype>, List<RichTextLabel>> _jobs = [];

    private ClientGameTicker? _gameTicker;
    private SpriteSystem? _sprite;

    private NetEntity? _selectedStation;

    [ViewVariables]
    private SLWindow? _window;

    protected override void Open() => UpdateState(State);
    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        TryInitWindow();

        if (!_window!.IsOpen)
            _window.OpenCentered();
    }

    private void TryInitWindow()
    {
        if (_window != null) return;
        var departments = _protos.EnumeratePrototypes<DepartmentPrototype>().Where(x => !x.EditorHidden).OrderByDescending(x => x.Weight);
        var jobsDict = _protos.EnumeratePrototypes<JobPrototype>().ToDictionary(x => x.ID);
        _sprite ??= _entitySystem.GetEntitySystem<SpriteSystem>();
        _gameTicker ??= _entitySystem.GetEntitySystem<ClientGameTicker>();
        _gameTicker.LobbyJobsAvailableUpdated += Render;
        _jobs.Clear();

        if (_selectedStation == null && _gameTicker.StationNames.Count > 0)
            _selectedStation = _gameTicker.StationNames.First().Key;
        _window = new SLWindow()
        {
            MinSize = new Vector2(756, 512),
            MaxSize = new Vector2(757, 513),
        }
        .Style(x => x.Starlight)
        .Scroll(scroll => scroll
            .Box(BoxContainer.LayoutOrientation.Vertical, box => box
            .SelectBox<KeyValuePair<NetEntity, string>>(
                item => item.Value,
                select => select
                    .Bind(x => SelectStation(x.Key))
                .SetItems(_gameTicker.StationNames.ToList()))
            .Grid(5, x => x
                .HorizontalExp()
                .AddChildren(Table(jobsDict, departments)))));

        _window.OnClose += Close;
        _window.Title = "recruitment computer";
        Render();
    }

    private IEnumerable<Control> Table(IDictionary<string, JobPrototype> jobs, IEnumerable<DepartmentPrototype> departments)
    => departments.SelectMany(x => new Control[]
    {
        new SLStripe { EdgeColor = x.Color}
            .Add(new RichTextLabel {Text = $"[color={x.Color.ToHex()}][font size=14][bold]{Loc.GetString(x.Name)}[/bold][/font][/color]", HorizontalExpand = true}),
        new SLStripe { EdgeColor = x.Color},
        new SLStripe { EdgeColor = x.Color},
        new SLStripe { EdgeColor = x.Color},
        new SLStripe { EdgeColor = x.Color}
    }.Concat(InnerTable(x.Roles.Where(x=> x.Id != "StationAi").Select(r => jobs[r.Id]).OrderByDescending(x => x.Weight))));

    private IEnumerable<Control> InnerTable(IEnumerable<JobPrototype> jobs)
    => jobs.SelectMany(x => new Control[5]
    {
        new RichTextLabel { Text = $"[font size=10]{Loc.GetString(x.Name)}[/font]", HorizontalExpand = true},
        new TextureRect {
            TextureScale = new Vector2(2.5f, 2.5f),
            Texture = _protos.TryIndex(x.Icon, out var iconPrototype)
            ? _sprite!.Frame0(iconPrototype.Icon)
            : null},
        new TextureButton { SetSize = new Vector2(16,16), TexturePath ="/Textures/_Starlight/Interface/Nano/minus.png"}
        .OnClick(()=>SendMessage(new RecruitmentChangeBuiMsg()
        {
            Station = _selectedStation!.Value,
            Job = x.ID,
            Amount = -1
        })),
        new RichTextLabel { Text = $"[font size=10]0[/font]"}
            .SaveTo(c =>
            {
                if(_jobs.TryGetValue(x.ID, out var list))
                    list.Add(c);
                else
                    _jobs.Add(x.ID, new(){c});
            }),
        new TextureButton { SetSize = new Vector2(16,16), TexturePath ="/Textures/_Starlight/Interface/Nano/plus.png"}
        .OnClick(()=>SendMessage(new RecruitmentChangeBuiMsg()
        {
            Station = _selectedStation!.Value,
            Job = x.ID,
            Amount = +1
        })),
    });
    private void SelectStation(NetEntity key)
    {
        _selectedStation = key;
        Render();
    }
    private void Render(IReadOnlyDictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, int?>> _) => Render();
    private void Render()
    {
        if (_selectedStation == null
            || !_gameTicker!.JobsAvailable.TryGetValue(_selectedStation.Value, out var jobs))
            return;

        foreach (var (key, labels) in _jobs)
            foreach (var label in labels)
                label.Text = jobs.TryGetValue(key, out var amount)
                ? $"[font size=10]{amount}[/font]"
                : $"[font size=10]0[/font]";
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
