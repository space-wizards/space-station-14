using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.Preferences.UI;

public abstract class RequirementsSelector<T> : BoxContainer
{
    public T Proto { get; }
    public bool Disabled => _lockStripe.Visible;

    protected readonly RadioOptions<int> Options;
    private StripeBack _lockStripe;

    protected RequirementsSelector(T proto)
    {
        Proto = proto;

        Options = new RadioOptions<int>(RadioOptionsLayout.Horizontal)
        {
            FirstButtonStyle = StyleBase.ButtonOpenRight,
            ButtonStyle = StyleBase.ButtonOpenBoth,
            LastButtonStyle = StyleBase.ButtonOpenLeft
        };
        //Override default radio option button width
        Options.GenerateItem = GenerateButton;

        Options.OnItemSelected += args => Options.Select(args.Id);

        var requirementsLabel = new Label()
        {
            Text = Loc.GetString("role-timer-locked"),
            Visible = true,
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = {StyleBase.StyleClassLabelSubText},
        };

        _lockStripe = new StripeBack()
        {
            Visible = false,
            HorizontalExpand = true,
            MouseFilter = MouseFilterMode.Stop,
            Children =
            {
                requirementsLabel
            }
        };

        // Setup must be called after
    }

    /// <summary>
    /// Actually adds the controls, must be called in the inheriting class' constructor.
    /// </summary>
    protected void Setup((string, int)[] items, string title, int titleSize, string? description, TextureRect? icon = null)
    {
        foreach (var (text, value) in items)
        {
            Options.AddItem(Loc.GetString(text), value);
        }

        var titleLabel = new Label()
        {
            Margin = new Thickness(5f, 0, 5f, 0),
            Text = title,
            MinSize = new Vector2(titleSize, 0),
            MouseFilter = MouseFilterMode.Stop,
            ToolTip = description
        };

        if (icon != null)
            AddChild(icon);
                
        AddChild(titleLabel);
        AddChild(Options);
        AddChild(_lockStripe);
    }

    public void LockRequirements(FormattedMessage requirements)
    {
        var tooltip = new Tooltip();
        tooltip.SetMessage(requirements);
        _lockStripe.TooltipSupplier = _ => tooltip;
        _lockStripe.Visible = true;
        Options.Visible = false;
    }

    // TODO: Subscribe to roletimers event. I am too lazy to do this RN But I doubt most people will notice fn
    public void UnlockRequirements()
    {
        _lockStripe.Visible = false;
        Options.Visible = true;
    }

    private Button GenerateButton(string text, int value)
    {
        return new Button
        {
            Text = text,
            MinWidth = 90
        };
    }
}
