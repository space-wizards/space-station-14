using Content.Replay.Observer;
using DiscordRPC.Message;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.IO;
using System.Linq;

namespace Content.Replay.Manager;

// This partial class contains code to read the replay files.
public sealed partial class ReplayManager
{

    public static readonly string YamlFilename = "replay.yml";

    public void LoadReplay(IWritableDirProvider dir)
    {
        if (CurrentReplay != null)
            StopReplay();

        CurrentReplay = InternalLoadReplay(dir);
        if (CurrentReplay == null)
            return;

        ResetToNearestCheckpoint(0, true);
        _entMan.EntitySysManager.GetEntitySystem<ReplayObserverSystem>().SetObserverPosition(default);
        _controller.ContentEntityTickUpdate += TickUpdate;
        RegisterCommands();
    }

    private ReplayData InternalLoadReplay(IWritableDirProvider directory)
    {
        List<GameState> states = new();
        List<ReplayMessage> messages = new();

        var compressionContext = new ZStdCompressionContext();
        var metaData = LoadMetadata(directory);

        int i = 0;
        var name = new ResourcePath("0.dat").ToRootedPath();
        var intBuf = new byte[4];
        int uncompressedSize;

        while (directory.Exists(name))
        {
            using var fileStream = directory.OpenRead(name);
            using var decompressStream = new ZStdDecompressStream(fileStream, false);

            fileStream.Read(intBuf);
            uncompressedSize = BitConverter.ToInt32(intBuf);

            var decompressedStream = new MemoryStream(uncompressedSize);
            decompressStream.CopyTo(decompressedStream, uncompressedSize);
            decompressedStream.Position = 0;

            while (decompressedStream.Position < decompressedStream.Length)
            {
                _serializer.DeserializeDirect(decompressedStream, out GameState state);
                _serializer.DeserializeDirect(decompressedStream, out ReplayMessage msg);
                states.Add(state);
                messages.Add(msg);
            }

            i++;
            name = new ResourcePath($"{i}.dat").ToRootedPath();
        }

        compressionContext.Dispose();
        var checkpoints = GenerateCheckpoints(metaData.CVars, states, messages);

        return new(states, messages, states[0].ToSequence, metaData.StartTime, metaData.Duration, checkpoints);
    }

    private (HashSet<string> CVars, TimeSpan Duration, TimeSpan StartTime) LoadMetadata(IWritableDirProvider directory)
    {
        using var file = directory.OpenRead(new ResourcePath(YamlFilename).ToRootedPath());
        var data = (MappingDataNode) DataNodeParser.ParseYamlStream(new StreamReader(file)).First().Root!;

        var typeHash = Convert.FromHexString(((ValueDataNode) data["typeHash"]).Value);
        var stringHash = Convert.FromHexString(((ValueDataNode) data["stringHash"]).Value);
        var startTick = ((ValueDataNode) data["startTick"]).Value;
        var timeBaseTick = ((ValueDataNode) data["timeBaseTick"]).Value;
        var timeBaseTimespan = ((ValueDataNode) data["timeBaseTimespan"]).Value;
        var duration = TimeSpan.Parse(((ValueDataNode) data["duration"]).Value);

        if (!typeHash.SequenceEqual(_serializer.GetSerializableTypesHash()))
        {
            // TODO REPLAYS make this show an informative pop-up instead of just throwing an exception.
            throw new Exception($"{nameof(IRobustSerializer)} hashes do not match. Loading replays using a bad replay-client version?");
        }

        using var stringFile = directory.OpenRead(new ResourcePath("strings.dat").ToRootedPath());
        var stringData = new byte[stringFile.Length];
        stringFile.Read(stringData);
        _serializer.SetStringSerializerPackage(stringHash, stringData);

        using var cvarsFile = directory.OpenRead(new ResourcePath("cvars.toml").ToRootedPath());
        // Note, this does not invoke the received-initial-cvars event. But at least currently, that doesn't matter
        var cvars = _netConf.LoadFromTomlStream(cvarsFile);

        _timing.CurTick = new GameTick(uint.Parse(startTick));
        _timing.TimeBase = (new TimeSpan(long.Parse(timeBaseTimespan)), new GameTick(uint.Parse(timeBaseTick)));

        var initFile = new ResourcePath("init_messages.dat").ToRootedPath();
        if (directory.Exists(initFile))
        {
            using var initMessageFile = directory.OpenRead(initFile);
            _serializer.DeserializeDirect(initMessageFile, out ReplayMessage initMessages);
            ProcessMessages(initMessages, false);
        }

        return (cvars, duration, _timing.CurTime);
    }
}
