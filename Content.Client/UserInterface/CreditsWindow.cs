#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared;
using Robust.Client.Credits;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.UserInterface
{
    public sealed class CreditsWindow : SS14Window
    {
        [Dependency] private readonly IResourceCache _resourceManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private static readonly Dictionary<string, int> PatronTierPriority = new()
        {
            ["Nuclear Operative"] = 1,
            ["Syndicate Agent"] = 2,
            ["Revolutionary"] = 3
        };

        public CreditsWindow()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Credits");

            var rootContainer = new TabContainer();

            var patronsList = new ScrollContainer
            {
                HScrollEnabled = false
            };
            var ss14ContributorsList = new ScrollContainer
            {
                HScrollEnabled = false
            };
            var licensesList = new ScrollContainer
            {
                HScrollEnabled = false
            };

            rootContainer.AddChild(ss14ContributorsList);
            rootContainer.AddChild(patronsList);
            rootContainer.AddChild(licensesList);

            TabContainer.SetTabTitle(patronsList, Loc.GetString("Patrons"));
            TabContainer.SetTabTitle(ss14ContributorsList, Loc.GetString("Credits"));
            TabContainer.SetTabTitle(licensesList, Loc.GetString("Open Source Licenses"));

            PopulatePatronsList(patronsList);
            PopulateCredits(ss14ContributorsList);
            PopulateLicenses(licensesList);

            Contents.AddChild(rootContainer);

            SetSize = (650, 650);
        }

        private void PopulateLicenses(ScrollContainer licensesList)
        {
            var vBox = new VBoxContainer
            {
                Margin = new Thickness(2, 2, 0, 0)
            };

            foreach (var entry in CreditsManager.GetLicenses().OrderBy(p => p.Name))
            {
                vBox.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = entry.Name});

                // We split these line by line because otherwise
                // the LGPL causes Clyde to go out of bounds in the rendering code.
                foreach (var line in entry.License.Split("\n"))
                {
                    vBox.AddChild(new Label {Text = line, FontColorOverride = new Color(200, 200, 200)});
                }
            }

            licensesList.AddChild(vBox);
        }

        private void PopulatePatronsList(Control patronsList)
        {
            var vBox = new VBoxContainer
            {
                Margin = new Thickness(2, 2, 0, 0)
            };
            var patrons = LoadPatrons();

            // Do not show "become a patron" button on Steam builds
            // since Patreon violates Valve's rules about alternative storefronts.
            if (!_cfg.GetCVar(CCVars.BrandingSteam))
            {
                Button patronButton;
                vBox.AddChild(patronButton = new Button
                {
                    Text = "Become a Patron",
                    HorizontalAlignment = HAlignment.Center
                });

                patronButton.OnPressed +=
                    _ => IoCManager.Resolve<IUriOpener>().OpenUri(UILinks.Patreon);
            }

            var first = true;
            foreach (var tier in patrons.GroupBy(p => p.Tier).OrderBy(p => PatronTierPriority[p.Key]))
            {
                if (!first)
                {
                    vBox.AddChild(new Control {MinSize = (0, 10)});
                }

                first = false;
                vBox.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = $"{tier.Key}"});

                var msg = string.Join(", ", tier.OrderBy(p => p.Name).Select(p => p.Name));

                var label = new RichTextLabel();
                label.SetMessage(msg);

                vBox.AddChild(label);
            }



            patronsList.AddChild(vBox);
        }

        private IEnumerable<PatronEntry> LoadPatrons()
        {
            var yamlStream = _resourceManager.ContentFileReadYaml(new ResourcePath("/Credits/Patrons.yml"));
            var sequence = (YamlSequenceNode) yamlStream.Documents[0].RootNode;

            return sequence
                .Cast<YamlMappingNode>()
                .Select(m => new PatronEntry(m["Name"].AsString(), m["Tier"].AsString()));
        }

        private void PopulateCredits(Control contributorsList)
        {
            Button contributeButton;

            var vBox = new VBoxContainer
            {
                Margin = new Thickness(2, 2, 0, 0)
            };

            vBox.AddChild(new HBoxContainer
            {
                HorizontalAlignment = HAlignment.Center,
                SeparationOverride = 20,
                Children =
                {
                    new Label {Text = "Want to get on this list?"},
                    (contributeButton = new Button {Text = "Contribute!"})
                }
            });

            var first = true;

            void AddSection(string title, string path, bool markup = false)
            {
                if (!first)
                {
                    vBox.AddChild(new Control {MinSize = (0, 10)});
                }

                first = false;
                vBox.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = title});

                var label = new RichTextLabel();
                var text = _resourceManager.ContentFileReadAllText($"/Credits/{path}");
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

            AddSection("Space Station 14 Contributors", "GitHub.txt");
            AddSection("Space Station 13 Codebases", "SpaceStation13.txt");
            AddSection("Original Space Station 13 Remake Team", "OriginalRemake.txt");
            AddSection("Special Thanks", "SpecialThanks.txt", true);

            contributorsList.AddChild(vBox);

            contributeButton.OnPressed += _ =>
                IoCManager.Resolve<IUriOpener>().OpenUri(UILinks.GitHub);
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

        private sealed class PatronEntry
        {
            public string Name { get; }
            public string Tier { get; }

            public PatronEntry(string name, string tier)
            {
                Name = name;
                Tier = tier;
            }
        }
    }
}
