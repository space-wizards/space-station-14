using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Kitchen.UI;

[UsedImplicitly]
public sealed partial class MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [Dependency] private SharedMicrowaveSystem _microwave = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [ViewVariables]
    private MicrowaveMenu? _menu;

    [ViewVariables]
    private readonly Dictionary<int, EntityUid> _solids = new();

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

        RefreshContentsDisplay();
        UpdateActiveDisplay(comp);

        // Update the currently-selected cook time label and button
        var buttonIndex = comp.CurrentCookTimeButtonIndex;
        if (_menu.TryGetCookTimeButton(buttonIndex, out var button))
            button.Pressed = true;

        var timeLabel = buttonIndex == 0
            ? Loc.GetString("microwave-menu-instant-button")
            : comp.CurrentCookTimerTime.ToString();

        _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
            ("time", timeLabel));
    }

    /// <summary>
    ///     Update the state of various controls in this menu based on the active / empty status of the microwave.
    /// </summary>
    /// <param name="comp">The microwave component associated with this entity.</param>
    private void UpdateActiveDisplay(MicrowaveComponent? comp)
    {
        if (_menu is null)
            return;

        // Disable various UI controls if the microwave is active or empty
        var isActive = EntMan.TryGetComponent<ActiveMicrowaveComponent>(Owner, out var activeComp);
        var isEmpty = !_microwave.HasContents((Owner, comp));
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

    /// <summary>
    ///     Update the panel containing all of the microwave's contents.
    /// </summary>
    private void RefreshContentsDisplay()
    {
        if (_menu == null)
            return;

        _solids.Clear();
        _menu.IngredientsList.Clear();
        var containedSolids = _microwave.GetMicrowaveContents(Owner);

        foreach (var entity in containedSolids)
        {
            if (EntMan.Deleted(entity))
                continue;

            // TODO just use sprite view
            var itemIcon = GetEntityIcon(entity);
            var itemName = EntMan.GetComponent<MetaDataComponent>(entity).EntityName;
            var solidItem = _menu.IngredientsList.AddItem(itemName, itemIcon);
            var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
            _solids.Add(solidIndex, entity);
        }
    }

    /// <summary>
    ///     Get the texture associated with an ingredient.
    /// </summary>
    /// <param name="uid">The ingredient entity.</param>
    private Texture? GetEntityIcon(EntityUid uid)
    {
        if (EntMan.TryGetComponent<IconComponent>(uid, out var icon))
            return _sprite.GetIcon(icon);

        if (EntMan.TryGetComponent<SpriteComponent>(uid, out var sprite))
            return sprite.Icon?.Default;

        return null;
    }
}
