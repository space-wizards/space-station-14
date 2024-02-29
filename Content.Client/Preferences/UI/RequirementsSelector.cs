using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Preferences.UI;

public abstract class RequirementsSelector<T> : BoxContainer where T : IPrototype
{
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

        var loadoutWindowBtn = new Button()
        {
            Text = Loc.GetString("loadout-window"),
            HorizontalExpand = true,
            Group = _loadoutGroup,
        };

        // TODO: Attach to profile / jobs.
        var collection = IoCManager.Instance!;
        var entManager = collection.Resolve<IEntityManager>();
        var protoManager = collection.Resolve<IPrototypeManager>();
        protoManager.TryIndex(_prefix + Proto.ID, out RoleLoadoutPrototype? loadoutProto);

        // If no loadout found then disabled button
        if (loadoutProto == null)
        {
            loadoutWindowBtn.Disabled = true;
        }
        // else
        else
        {
            var session = collection.Resolve<IPlayerManager>().LocalSession!;
            var loadout = new RoleLoadout((ProtoId<RoleLoadoutPrototype>) loadoutProto.ID);
            loadout.SetDefault(entManager, protoManager);
            loadout.EnsureValid(session, collection);

            loadoutWindowBtn.OnPressed += args =>
            {
                if (args.Button.Pressed)
                {
                    _loadout = new LoadoutWindow(loadout, loadoutProto, session, collection)
                    {
                        Title = Loc.GetString(_prefix + Proto.ID + "-loadout"),
                    };

                    _loadout.RefreshLoadouts(loadout, session, collection);

                    _loadout.OnLoadoutPressed += (selectedGroup, selectedLoadout) =>
                    {
                        loadout.ApplyLoadout(selectedGroup, selectedLoadout, entManager);
                        _loadout.RefreshLoadouts(loadout, session, collection);
                    };

                    _loadout.OpenCenteredLeft();
                    _loadout.OnClose += () =>
                    {
                        loadoutWindowBtn.Pressed = false;
                    };
                }
                else
                {
                    _loadout?.Close();
                    _loadout = null;
                }
            };
        }

        AddChild(loadoutWindowBtn);
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
