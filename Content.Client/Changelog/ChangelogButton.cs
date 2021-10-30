using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Changelog
{
    public sealed class ChangelogButton : Button
    {
        [Dependency] private readonly ChangelogManager _changelogManager = default!;

        public ChangelogButton()
        {
            IoCManager.InjectDependencies(this);

            OnPressed += OnOnPressed;

            // So that measuring before opening returns a correct height,
            // and the window has the correct size when opened.
            Text = " ";
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _changelogManager.NewChangelogEntriesChanged += UpdateStuff;
            UpdateStuff();
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _changelogManager.NewChangelogEntriesChanged -= UpdateStuff;
        }

        private void OnOnPressed(ButtonEventArgs obj)
        {
            new ChangelogWindow().OpenCentered();
        }

        private void UpdateStuff()
        {
            if (_changelogManager.NewChangelogEntries)
            {
                Text = Loc.GetString("changelog-button-new-entries");
                StyleClasses.Add(StyleBase.ButtonCaution);
            }
            else
            {
                Text = Loc.GetString("changelog-button");
                StyleClasses.Remove(StyleBase.ButtonCaution);
            }
        }
    }
}
