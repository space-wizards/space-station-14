using Content.Shared.DeadSpace.Ports.Jukebox;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.Ports.Jukebox;

public sealed class ServerJukeboxSongsSyncManager : JukeboxSongsSyncManager
{
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _netManager.Connected += OnClientConnected;
    }

    private void OnClientConnected(object? sender, NetChannelArgs e)
    {
        foreach (var (path, data) in ContentRoot.GetAllFiles())
        {
            var msg = new JukeboxSongUploadNetMessage
            {
                RelativePath = path,
                Data = data
            };

            e.Channel.SendMessage(msg);
        }
    }

    public (string SongName, ResPath Path) SyncSongData(string songName, List<byte> bytes)
    {
        if (ContentRoot.TryGetFile(new ResPath(songName + ".ogg"), out _))
        {
            songName += "a";
        }

        var msg = new JukeboxSongUploadNetMessage()
        {
            Data = bytes.ToArray(),
            RelativePath = new ResPath(songName + ".ogg")
        };

        OnSongUploaded(msg);
        var path = new ResPath($"{Prefix}/{songName}.ogg");
        return (songName, path);

    }


    public override void OnSongUploaded(JukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath, message.Data);

        foreach (var channel in _netManager.Channels)
        {
            channel.SendMessage(message);
        }
    }

    public void CleanUp()
    {
        ContentRoot.Clear();
    }
}
