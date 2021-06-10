#nullable enable

using System.Collections.Generic;
using System.IO;
using Content.Client.EscapeMenu.UI;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Info
{
    public sealed class InfoWindow : SS14Window
    {
        [Dependency] private readonly IResourceCache _resourceManager = default!;

        private OptionsMenu optionsMenu;

        public InfoWindow()
        {
            IoCManager.InjectDependencies(this);

            optionsMenu = new OptionsMenu();

            Title = Loc.GetString("ui-info-title");

            var rootContainer = new TabContainer();

            var rulesList = new ScrollContainer
            {
                HScrollEnabled = false
            };
            var tutorialList = new ScrollContainer
            {
                HScrollEnabled = false
            };


            rootContainer.AddChild(rulesList);
            rootContainer.AddChild(tutorialList);

            TabContainer.SetTabTitle(rulesList, Loc.GetString("ui-info-tab-rules"));
            TabContainer.SetTabTitle(tutorialList, Loc.GetString("ui-info-tab-tutorial"));

            PopulateRules(rulesList);
            PopulateTutorial(tutorialList);

            Contents.AddChild(rootContainer);

            SetSize = (650, 650);
        }

        private void PopulateRules(Control rulesList)
        {
            var vBox = new VBoxContainer
            {
                Margin = new Thickness(2, 2, 0, 0)
            };

            var first = true;

            void AddSection(string title, string path, bool markup = false)
            {
                if (!first)
                {
                    vBox.AddChild(new Control { MinSize = (0, 10) });
                }

                first = false;
                vBox.AddChild(new Label { StyleClasses = { StyleBase.StyleClassLabelHeading }, Text = title });

                var label = new RichTextLabel();
                var text = _resourceManager.ContentFileReadAllText($"/Server Info/{path}");
                if (markup)
                {
                    label.SetMessage(FormattedMessage.FromMarkup(text.Trim()));
                }
                else
                {
                    label.SetMessage(text);
                }

                vBox.AddChild(label);
            }

            AddSection(Loc.GetString("ui-info-header-rules"), "Rules.txt", true);

            rulesList.AddChild(vBox);

        }

        private void PopulateTutorial(Control tutorialList)
        {
            Button controlsButton;

            var vBox = new VBoxContainer
            {
                Margin = new Thickness(2, 2, 0, 0)
            };

            var first = true;

            void AddSection(string title, string path, bool markup = false)
            {
                if (!first)
                {
                    vBox.AddChild(new Control { MinSize = (0, 10) });
                }

                first = false;
                vBox.AddChild(new Label { StyleClasses = { StyleBase.StyleClassLabelHeading }, Text = title });

                var label = new RichTextLabel();
                var text = _resourceManager.ContentFileReadAllText($"/Server Info/{path}");
                if (markup)
                {
                    label.SetMessage(FormattedMessage.FromMarkup(text.Trim()));
                }
                else
                {
                    label.SetMessage(text);
                }

                vBox.AddChild(label);
            }

            AddSection(Loc.GetString("ui-info-header-intro"), "Intro.txt");

            vBox.AddChild(new HBoxContainer
            {
                MinSize = (0, 10),
                Children =
                {
                    new Label {StyleClasses = { StyleBase.StyleClassLabelHeading }, Text = Loc.GetString("ui-info-header-controls")},
                }
            });

            vBox.AddChild(new HBoxContainer
            {
                SeparationOverride = 5,
                Children =
                {
                     new Label {Text = Loc.GetString("ui-info-text-controls")},
                     (controlsButton = new Button {Text = Loc.GetString("ui-info-button-controls")})
                }
            });

            AddSection(Loc.GetString("ui-info-header-gameplay"), "Gameplay.txt", true);
            AddSection(Loc.GetString("ui-info-header-sandbox"), "Sandbox.txt", true);

            tutorialList.AddChild(vBox);

            controlsButton.OnPressed += _ =>
                optionsMenu.OpenCentered();
        }

        private static IEnumerable<string> Lines(TextReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    yield break;
                }

                yield return line;
            }
        }
    }
}
