using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Preferences.Loadouts;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Preferences.UI;

public abstract class RequirementsSelector<T> : BoxContainer where T : IPrototype
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    /// <summary>
    /// Prefix for prototypes.
    /// </summary>
    private string _prefix;

    private ButtonGroup _loadoutGroup;

    public T Proto { get; }
    public bool Disabled => _lockStripe.Visible;

    protected readonly RadioOptions<int> Options;
    private readonly StripeBack _lockStripe;
    private LoadoutWindow? _loadout;

    protected RequirementsSelector(string prefix, T proto, ButtonGroup loadoutGroup)
    {
        IoCManager.InjectDependencies(this);
        _loadoutGroup = loadoutGroup;
        _prefix = prefix;
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

        var loadout = new Button()
        {
            ToggleMode = true,
            Text = Loc.GetString("loadout-window"),
            HorizontalExpand = true,
            Group = _loadoutGroup,
        };

        _protoManager.TryIndex(_prefix + Proto.ID, out RoleLoadoutPrototype? loadoutProto);

        // If no loadout found then disabled button
        if (loadoutProto == null)
        {
            loadout.Disabled = true;
        }
        // else
        else
        {
            loadout.OnPressed += args =>
            {
                if (args.Button.Pressed)
                {
                    _loadout = new LoadoutWindow(loadoutProto, _protoManager)
                    {
                        Title = Loc.GetString(_prefix + Proto.ID + "-loadout"),
                    };
                    _loadout.OpenCenteredLeft();
                    _loadout.OnClose += () =>
                    {
                        loadout.Pressed = false;
                    };
                }
                else
                {
                    _loadout?.Close();
                    _loadout = null;
                }
            };
        }

        AddChild(loadout);
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
