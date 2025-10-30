using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.DeadSpace.Ports.Jukebox;

[CVarDefs]
public sealed class JKCVars
{
    public static readonly CVarDef<float> MaxJukeboxSongSizeInMB = CVarDef.Create("jk.max_jukebox_song_size",
        3.5f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> MaxJukeboxSoundRange = CVarDef.Create("jk.max_jukebox_sound_range", 20f,
        CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}
