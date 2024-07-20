using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Shared.Guidebook;
using Content.Shared.Info;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class InfoUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    private RulesPopup? _rulesPopup;
    private RulesAndInfoWindow? _infoWindow;
    private ISawmill _sawmill = default!;

    [ValidatePrototypeId<GuideEntryPrototype>]
    private const string DefaultRuleset = "DefaultRuleset";

    public ProtoId<GuideEntryPrototype> RulesEntryId = DefaultRuleset;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logMan.GetSawmill("rules");
        _netManager.RegisterNetMessage<RulesAcceptedMessage>();
        _netManager.RegisterNetMessage<SendRulesInformationMessage>(OnRulesInformationMessage);

        _consoleHost.RegisterCommand("fuckrules",
            "",
            "",
            (_, _, _) =>
        {
            OnAcceptPressed();
        });
    }

    private void OnRulesInformationMessage(SendRulesInformationMessage message)
    {
        RulesEntryId = message.CoreRules;

        if (message.ShouldShowRules)
            ShowRules(message.PopupTime);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_infoWindow == null)
            return;

        _infoWindow.Dispose();
        _infoWindow = null;
    }

    private void ShowRules(float time)
    {
        if (_rulesPopup != null)
            return;

        _rulesPopup = new RulesPopup
        {
            Timer = time
        };

        _rulesPopup.OnQuitPressed += OnQuitPressed;
        _rulesPopup.OnAcceptPressed += OnAcceptPressed;
        UIManager.WindowRoot.AddChild(_rulesPopup);
        LayoutContainer.SetAnchorPreset(_rulesPopup, LayoutContainer.LayoutPreset.Wide);
    }

    private void OnQuitPressed()
    {
        _consoleHost.ExecuteCommand("quit");
    }

    private void OnAcceptPressed()
    {
        _netManager.ClientSendMessage(new RulesAcceptedMessage());

        _rulesPopup?.Orphan();
        _rulesPopup = null;
    }

    public GuideEntryPrototype GetCoreRuleEntry()
    {
        if (!_prototype.TryIndex(RulesEntryId, out var guideEntryPrototype))
        {
            guideEntryPrototype = _prototype.Index<GuideEntryPrototype>(DefaultRuleset);
            _sawmill.Error($"Couldn't find the following prototype: {RulesEntryId}. Falling back to {DefaultRuleset}, please check that the server has the rules set up correctly");
            return guideEntryPrototype;
        }

        return guideEntryPrototype;
    }

    public void OpenWindow()
    {
        if (_infoWindow == null || _infoWindow.Disposed)
            _infoWindow = UIManager.CreateWindow<RulesAndInfoWindow>();

        _infoWindow?.OpenCentered();
    }
}
