using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Photography;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoFilmComponent : SharedPhotoFilmComponent, IExamine
    {
        private int _film = 10;
        private int _filmMax = 10;

        [ViewVariables]
        public int Film
        {
            get => _film;
            set
            {
                _film = value;
                Dirty();
            }
        }
        [ViewVariables] public int FilmMax
        {
            get => _filmMax;
            set
            {
                _filmMax = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Dirty();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _film, "film", 10);
            serializer.DataField(ref _filmMax, "maxfilm", 10);
        }

        public override ComponentState GetComponentState()
        {
            return new PhotoFilmComponentState(Film, FilmMax);
        }

        public bool TakeFilm(int take, out int took)
        {
            if (take <= 0)
            {
                took = 0;
                return false;
            }

            if(_film >= take)
            {
                took = take;
                Film -= take;
            } else
            {
                took = _film;
                Film = 0;
            }

            return _film == 0;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetString("Photos: [color=white]{0}/{1}[/color]", _film, _filmMax));
            }
        }
    }
}
