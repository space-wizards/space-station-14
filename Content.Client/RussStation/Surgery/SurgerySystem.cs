using Content.Client.UserInterface.Controls;
using Content.Shared.RussStation.Surgery;
using Content.Shared.RussStation.Surgery.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.RussStation.Surgery;

public sealed class SurgerySystem : SharedSurgerySystem
{
    private SimpleRadialMenu? _menu;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<OpenSurgeryMenuEvent>(OnOpenSurgeryMenu);
        SubscribeNetworkEvent<OpenOrganMenuEvent>(OnOpenOrganMenu);
    }

    private void OnOpenSurgeryMenu(OpenSurgeryMenuEvent ev)
    {
        CloseMenu();

        var buttons = new List<RadialMenuOptionBase>();

        foreach (var procedureId in ev.ProcedureIds)
        {
            if (!ProtoManager.TryIndex<SurgeryProcedurePrototype>(procedureId, out var proto))
            {
                Log.Warning($"Server sent unknown surgery procedure prototype: {procedureId}");
                continue;
            }

            var id = procedureId;
            var target = ev.Target;
            var bedsheet = ev.Bedsheet;

            buttons.Add(new RadialMenuActionOption<string>(
                _ => OnProcedureSelected(target, bedsheet, id),
                id)
            {
                ToolTip = Loc.GetString(proto.Name),
                IconSpecifier = RadialMenuIconSpecifier.With(
                    new SpriteSpecifier.Rsi(
                        new ResPath("Objects/Specific/Medical/Surgery/scalpel.rsi"),
                        "scalpel")),
            });
        }

        if (buttons.Count == 0)
            return;

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(buttons);
        _menu.OpenOverMouseScreenPosition();
    }

    private void OnProcedureSelected(NetEntity target, NetEntity bedsheet, string procedureId)
    {
        CloseMenu();
        RaiseNetworkEvent(new SelectSurgeryProcedureEvent(target, bedsheet, procedureId));
    }

    private void OnOpenOrganMenu(OpenOrganMenuEvent ev)
    {
        CloseMenu();

        var buttons = new List<RadialMenuOptionBase>();

        foreach (var (organId, name, protoId) in ev.Organs)
        {
            var id = organId;
            var target = ev.Target;

            var option = new RadialMenuActionOption<NetEntity>(
                _ => OnOrganSelected(target, id),
                id)
            {
                ToolTip = protoId != null ? $"{name} ({protoId})" : name,
            };

            if (protoId != null)
            {
                option.IconSpecifier = RadialMenuIconSpecifier.With(
                    new SpriteSpecifier.EntityPrototype(protoId));
            }

            buttons.Add(option);
        }

        if (buttons.Count == 0)
            return;

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(buttons);
        _menu.OpenOverMouseScreenPosition();
    }

    private void OnOrganSelected(NetEntity target, NetEntity organId)
    {
        CloseMenu();
        RaiseNetworkEvent(new SelectOrganEvent(target, organId));
    }

    private void CloseMenu()
    {
        _menu?.Close();
        _menu = null;
    }
}
