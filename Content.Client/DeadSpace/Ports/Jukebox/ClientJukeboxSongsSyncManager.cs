using Content.Shared.DeadSpace.Ports.Jukebox;

namespace Content.Client.DeadSpace.Ports.Jukebox;

public sealed class ClientJukeboxSongsSyncManager : JukeboxSongsSyncManager
{
    public override void OnSongUploaded(JukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath!, message.Data);
    }
}
