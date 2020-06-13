using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.GameObjects.Components.Photography
{
    public class SharedPhotoFilmComponent : Component
    {
        public override string Name => "PhotoFilm";
        public override uint? NetID => ContentNetIDs.PHOTO_FILM;
    }

    [NetSerializable, Serializable]
    public class PhotoFilmComponentState : ComponentState
    {
        public readonly int Film;
        public readonly int FilmMax;
        public PhotoFilmComponentState(int film, int filmMax) : base(ContentNetIDs.PHOTO_FILM)
        {
            Film = film;
            FilmMax = filmMax;
        }
    }
}
