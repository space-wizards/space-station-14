using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Effects;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Kitchen.UI;

[UsedImplicitly]
public sealed class MicrowaveBoundUserInterface : BoundUserInterface
{
    [Dependency] private SharedMicrowaveSystem _microwave = default!;

    [ViewVariables]
    private MicrowaveMenu? _menu;

    [ViewVariables]
    private readonly Dictionary<int, EntityUid> _solids = new();

    [ViewVariables]
    private readonly Dictionary<int, ReagentQuantity> _reagents = new();

    public MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<MicrowaveMenu>();
        _menu.StartButton.OnPressed += _ => SendPredictedMessage(new MicrowaveStartCookMessage());
        _menu.EjectButton.OnPressed += _ => SendPredictedMessage(new MicrowaveEjectMessage());
        _menu.IngredientsList.OnItemSelected += args =>
        {
            SendPredictedMessage(new MicrowaveEjectSolidIndexedMessage(EntMan.GetNetEntity(_solids[args.ItemIndex])));
        };

        _menu.OnCookTimeSelected += (args, buttonIndex) =>
        {
            var selectedCookTime = (uint)0;

            if (args.Button is MicrowaveMenu.MicrowaveCookTimeButton microwaveCookTimeButton)
            {
                // args.Button is a MicrowaveCookTimeButton
                var actualButton = (MicrowaveMenu.MicrowaveCookTimeButton)args.Button;
                selectedCookTime = actualButton.CookTime == 0 ? 0 : actualButton.CookTime;
                // SendMessage(new MicrowaveSelectCookTimeMessage((int) selectedCookTime / 5, actualButton.CookTime));
                SendPredictedMessage(new MicrowaveSelectCookTimeMessage((int)selectedCookTime / 5, actualButton.CookTime));

                _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                                ("time", selectedCookTime));
            }
            else
            {
                // args.Button is a normal button aka instant cook button
                SendPredictedMessage(new MicrowaveSelectCookTimeMessage((int)selectedCookTime, 0));

                _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                     ("time", Loc.GetString("microwave-menu-instant-button")));
            }
        };
    }

    public override void Update()
    {
        base.Update();

        if (_menu is null || !EntMan.TryGetComponent<MicrowaveComponent>(Owner, out var comp))
            return;

        var microwave = (Owner, comp);

        // TODO move this to a component state and ensure the net ids.
        var contents = _microwave.GetMicrowaveContents(microwave);
        RefreshContentsDisplay(contents.ToArray());

        // Update the currently-selected cook time label and button
        var buttonIndex = comp.CurrentCookTimeButtonIndex;
        if (_menu.TryGetCookTimeButton(buttonIndex, out var button))
            button.Pressed = true;

        var timeLabel = buttonIndex == 0 ? Loc.GetString("microwave-menu-instant-button") : comp.CurrentCookTimerTime.ToString();
        _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
            ("time", timeLabel));

        // Disable various UI controls if the microwave is active or empty
        var isActive = EntMan.TryGetComponent<ActiveMicrowaveComponent>(Owner, out var activeComp);
        var isEmpty = !_microwave.HasContents(microwave);
        var disableInteraction = isActive || isEmpty;
        _menu.IsBusy = isActive;
        _menu.ToggleBusyDisableOverlayPanel(disableInteraction);
        _menu.StartButton.Disabled = disableInteraction;
        _menu.EjectButton.Disabled = disableInteraction;

        if (activeComp != null)
            _menu.CurrentCooktimeEnd = activeComp.CookTimeEnd;

        // Set the "microwave light" panel color
        _menu.SetIngredientPanelLight(isActive && !isEmpty);
    }

    private void RefreshContentsDisplay(EntityUid[] containedSolids)
    {
        _reagents.Clear();

        if (_menu == null) return;

        _solids.Clear();
        _menu.IngredientsList.Clear();
        foreach (var entity in containedSolids)
        {
            if (EntMan.Deleted(entity))
            {
                return;
            }

            // TODO just use sprite view

            Texture? texture;
            if (EntMan.TryGetComponent<IconComponent>(entity, out var iconComponent))
            {
                texture = EntMan.System<SpriteSystem>().GetIcon(iconComponent);
            }
            else if (EntMan.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
            {
                texture = spriteComponent.Icon?.Default;
            }
            else
            {
                continue;
            }

            var solidItem = _menu.IngredientsList.AddItem(EntMan.GetComponent<MetaDataComponent>(entity).EntityName, texture);
            var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
            _solids.Add(solidIndex, entity);
        }
    }
}
