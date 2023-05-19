using System.Diagnostics.CodeAnalysis;
using Content.Replay.Observer;
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
using System.Threading.Tasks;
using Content.Replay.UI.Loading;
using Content.Replay.UI.Menu;

namespace Content.Replay.Manager;

// This partial class contains code to read the replay files.
public sealed partial class ReplayManager
{
    public static readonly string YamlFilename = "replay.yml";

    public void LoadReplay(IWritableDirProvider dir)
    {
        if (CurrentReplay != null)
            StopReplay();

        _controller.ContentEntityTickUpdate += TickUpdate;
        var screen = _stateMan.RequestStateChange<LoadingScreen<ReplayData>>();
        screen.Job = new LoadReplayJob(1/60f, dir, this, screen);
        screen.OnJobFinished += OnFinishedLoading;
    }

    private void OnFinishedLoading(ReplayData? data, Exception? ex)
    {
        if (data == null)
        {
            _controller.ContentEntityTickUpdate -= TickUpdate;
            _stateMan.RequestStateChange<ReplayMainScreen>();
            if (ex != null)
                _uiMan.Popup(Loc.GetString("main-menu-failed-to-connect", ("reason", ex)));
            return;
        }

        CurrentReplay = data;
        _entMan.EntitySysManager.GetEntitySystem<ReplayObserverSystem>().SetObserverPosition(default);
        RegisterCommands();
    }

    [SuppressMessage("ReSharper", "UseAwaitUsing")]
    internal async Task<ReplayData> InternalLoadReplay(IWritableDirProvider dir,
        Func<float, float, LoadReplayJob.LoadingState, bool, Task> callback)
    {
        List<GameState> states = new();
        List<ReplayMessage> messages = new();

        var compressionContext = new ZStdCompressionContext();
        var metaData = LoadMetadata(dir);

        var total = dir.Find("*.dat").files.Count();
        total--; // Exclude strings.dat

        if (dir.Exists(new ResPath("/init.dat")))
            total--;

        var i = 0;
        var intBuf = new byte[4];
        var name = new ResPath($"/{i++}.dat");
        while (dir.Exists(name))
        {
            await callback(i+1, total, LoadReplayJob.LoadingState.LoadingFiles, false);

            using var fileStream = dir.OpenRead(name);
            using var decompressStream = new ZStdDecompressStream(fileStream, false);

            fileStream.Read(intBuf);
            var uncompressedSize = BitConverter.ToInt32(intBuf);

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

            name = new ResPath($"/{i++}.dat");
        }
        DebugTools.Assert(i - 1 == total);
        await callback(total, total, LoadReplayJob.LoadingState.LoadingFiles, false);

        var initData = LoadInitFile(dir, compressionContext);
        compressionContext.Dispose();

        var checkpoints = await GenerateCheckpoints(initData, metaData.CVars, states, messages, callback);
        return new(states, messages, states[0].ToSequence, metaData.StartTime, metaData.Duration, checkpoints, initData);
    }

    private ReplayMessage? LoadInitFile(IWritableDirProvider dir, ZStdCompressionContext compressionContext)
    {
        var file = new ResPath("/init.dat");
        if (!dir.Exists(file))
            return null;

        // TODO compress init messages, then decompress them here.
        using var fileStream = dir.OpenRead(file);
        _serializer.DeserializeDirect(fileStream, out ReplayMessage initData);
        return initData;
    }

    public MappingDataNode? LoadYamlMetadata(IWritableDirProvider directory)
    {
        var path = new ResPath(YamlFilename).ToRootedPath();
        if (!directory.Exists(path))
            return null;

        using var file = directory.OpenRead(path);
        var parsed = DataNodeParser.ParseYamlStream(new StreamReader(file));
        return parsed.FirstOrDefault()?.Root as MappingDataNode;
    }

    public (HashSet<string> CVars, TimeSpan Duration, TimeSpan StartTime) LoadMetadata(IWritableDirProvider directory)
    {
        _sawmill.Info($"Reading replay metadata");
        var data = LoadYamlMetadata(directory);
        if (data == null)
            throw new Exception("Failed to parse yaml metadata");

        var typeHash = Convert.FromHexString(((ValueDataNode) data["typeHash"]).Value);
        var stringHash = Convert.FromHexString(((ValueDataNode) data["stringHash"]).Value);
        var startTick = ((ValueDataNode) data["startTick"]).Value;
        var timeBaseTick = ((ValueDataNode) data["timeBaseTick"]).Value;
        var timeBaseTimespan = ((ValueDataNode) data["timeBaseTimespan"]).Value;
        var duration = TimeSpan.Parse(((ValueDataNode) data["duration"]).Value);

        if (!typeHash.SequenceEqual(_serializer.GetSerializableTypesHash()))
            throw new Exception($"{nameof(IRobustSerializer)} hashes do not match. Loading replays using a bad replay-client version?");

        using var stringFile = directory.OpenRead(new ResPath("strings.dat").ToRootedPath());
        var stringData = new byte[stringFile.Length];
        stringFile.Read(stringData);
        _serializer.SetStringSerializerPackage(stringHash, stringData);

        using var cvarsFile = directory.OpenRead(new ResPath("cvars.toml").ToRootedPath());
        // Note, this does not invoke the received-initial-cvars event. But at least currently, that doesn't matter
        var cvars = _netConf.LoadFromTomlStream(cvarsFile);

        _timing.CurTick = new GameTick(uint.Parse(startTick));
        _timing.TimeBase = (new TimeSpan(long.Parse(timeBaseTimespan)), new GameTick(uint.Parse(timeBaseTick)));

        _sawmill.Info($"Successfully read metadata");
        return (cvars, duration, _timing.CurTime);
    }
}
