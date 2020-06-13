using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Photography;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoFilmComponent : SharedPhotoFilmComponent, IItemStatus
    {
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] public int Film { get; private set; } = 10;
        [ViewVariables] public int FilmMax { get; private set; } = 10;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is PhotoFilmComponentState film))
                return;

            Film = film.Film;
            FilmMax = film.FilmMax;
            _uiUpdateNeeded = true;
        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly PhotoFilmComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(PhotoFilmComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                var message = new FormattedMessage();
                message.AddMarkup(Loc.GetString("Photos: [color=white]{0}/{1}[/color]", _parent.Film, _parent.FilmMax));

                _label.SetMessage(message);
            }
        }
    }
}
