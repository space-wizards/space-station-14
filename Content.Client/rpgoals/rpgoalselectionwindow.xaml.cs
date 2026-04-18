using System.Numerics;
using Content.Shared.RPGoals;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;

namespace Content.Client.RPGoals;

public sealed class RPGoalSelectionWindow : DefaultWindow
{
    private readonly BoxContainer _optionsContainer;
    private readonly Button _rerollButton;

    public Action<string>? OnAcceptPressed;
    public Action? OnRerollPressed;

    public RPGoalSelectionWindow()
    {
        Title = Loc.GetString("rp-goals-ui-title");
        MinSize = new Vector2(540, 360);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(12),
        };

        root.AddChild(new Label { Text = Loc.GetString("rp-goals-ui-subtitle") });

        _optionsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };

        root.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            Children = { _optionsContainer }
        });

        _rerollButton = new Button();
        _rerollButton.OnPressed += _ => OnRerollPressed?.Invoke();
        root.AddChild(_rerollButton);

        Contents.AddChild(root);
    }

    public void UpdateState(RPGoalSelectionState state)
    {
        _optionsContainer.RemoveAllChildren();

        foreach (var option in state.Options)
        {
            var panel = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 4,
                Margin = new Thickness(8),
            };

            panel.AddChild(new Label
            {
                Text = option.Category,
            });

            panel.AddChild(new RichTextLabel
            {
                Text = Loc.GetString(option.LocaleKey),
            });

            var button = new Button
            {
                Text = Loc.GetString("rp-goals-ui-accept"),
                Disabled = state.Finalized,
            };
            button.OnPressed += _ => OnAcceptPressed?.Invoke(option.GoalId);

            panel.AddChild(button);
            var panelContainer = new PanelContainer();
            panelContainer.AddChild(panel);
            _optionsContainer.AddChild(panelContainer);
        }

        _rerollButton.Text = Loc.GetString("rp-goals-ui-reroll", ("count", state.RerollsRemaining));
        _rerollButton.Disabled = state.Finalized || state.RerollsRemaining <= 0;
    }
}
