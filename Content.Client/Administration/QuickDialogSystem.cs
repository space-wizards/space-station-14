using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration;

// mfw they ported input() from BYOND

/// <summary>
/// This handles the client portion of quick dialogs.
/// </summary>
public sealed class QuickDialogSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<QuickDialogOpenEvent>(OpenDialog);
    }

    private void OpenDialog(QuickDialogOpenEvent ev)
    {
        var window = new FancyWindow()
        {
            Title = ev.Title
        };

        var entryContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
        };

        var promptsDict = new Dictionary<string, LineEdit>();

        for (var index = 0; index < ev.Prompts.Count; index++)
        {
            var entry = ev.Prompts[index];
            var entryBox = new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal
            };

            entryBox.AddChild(new Label { Text = entry.Prompt, HorizontalExpand = true, SizeFlagsStretchRatio = 0.5f });
            var edit = new LineEdit() { HorizontalExpand = true };
            entryBox.AddChild(edit);
            switch (entry.Type)
            {
                case QuickDialogEntryType.Integer:
                    edit.IsValid += VerifyInt;
                    edit.PlaceHolder = Loc.GetString("quick-dialog-ui-integer");
                    break;
                case QuickDialogEntryType.Float:
                    edit.IsValid += VerifyFloat;
                    edit.PlaceHolder = Loc.GetString("quick-dialog-ui-float");
                    break;
                case QuickDialogEntryType.ShortText:
                    edit.IsValid += VerifyShortText;
                    edit.PlaceHolder = Loc.GetString("quick-dialog-ui-short-text");
                    break;
                case QuickDialogEntryType.LongText:
                    edit.IsValid += VerifyLongText;
                    edit.PlaceHolder = Loc.GetString("quick-dialog-ui-long-text");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            promptsDict.Add(entry.FieldId, edit);
            entryContainer.AddChild(entryBox);

            if (index == ev.Prompts.Count - 1)
            {
                // Last text box gets enter confirmation.
                // Only the last so you don't accidentally confirm early.
                edit.OnTextEntered += _ => Confirm();
            }
        }

        var buttonsBox = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = Control.HAlignment.Center,
        };

        var alreadyReplied = false;

        if ((ev.Buttons & QuickDialogButtonFlag.OkButton) != 0)
        {
            var okButton = new Button()
            {
                Text = Loc.GetString("quick-dialog-ui-ok"),
            };

            okButton.OnPressed += _ => Confirm();

            buttonsBox.AddChild(okButton);
        }

        if ((ev.Buttons & QuickDialogButtonFlag.OkButton) != 0)
        {
            var cancelButton = new Button()
            {
                Text = Loc.GetString("quick-dialog-ui-cancel"),
            };

            cancelButton.OnPressed += _ =>
            {
                RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                    new(),
                    QuickDialogButtonFlag.CancelButton));
                alreadyReplied = true;
                window.Close();
            };

            buttonsBox.AddChild(cancelButton);
        }

        window.OnClose += () =>
        {
            if (!alreadyReplied)
            {
                RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                    new(),
                    QuickDialogButtonFlag.CancelButton));
            }
        };

        entryContainer.AddChild(buttonsBox);

        window.ContentsContainer.AddChild(entryContainer);

        window.MinWidth *= 2; // Just double it.

        window.OpenCentered();

        return;

        void Confirm()
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                promptsDict.Select(x => (x.Key, x.Value.Text)).ToDictionary(x => x.Key, x => x.Text),
                QuickDialogButtonFlag.OkButton));
            alreadyReplied = true;
            window.Close();
        }
    }

    private bool VerifyInt(string input)
    {
        return int.TryParse(input, out var _);
    }

    private bool VerifyFloat(string input)
    {
        return float.TryParse(input, out var _);
    }

    private bool VerifyShortText(string input)
    {
        return input.Length <= 100;
    }

    private bool VerifyLongText(string input)
    {
        return input.Length <= 2000;
    }
}
