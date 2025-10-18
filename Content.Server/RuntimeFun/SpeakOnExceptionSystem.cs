using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.Server.RuntimeFun;

public sealed class SpeakOnExceptionSystem : EntitySystem
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // Special log handler that just saves the latest error.
    private SpeakOnExceptionLogHandler _logHandler = default!;

    public override void Initialize()
    {
        base.Initialize();

        _logHandler = new SpeakOnExceptionLogHandler();
        _log.RootSawmill.AddHandler(_logHandler);

        SubscribeLocalEvent<SpeakOnExceptionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpeakOnExceptionComponent, TransformSpeechEvent>(OnTransformSpeech, before: [ typeof(AccentSystem) ]);
    }

    private void OnMapInit(Entity<SpeakOnExceptionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextTimeCanSpeak = _timing.CurTime;

        // Make sure you don't speak when spawned if an error already occured
        ent.Comp.LastLog = _logHandler.LastLog;
    }

    public override void Update(float frameTime)
    {
        var log = _logHandler.LastLog;
        if (log == null)
            return;

        var query = EntityQueryEnumerator<SpeakOnExceptionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.NextTimeCanSpeak && log != comp.LastLog)
            {
                _chat.TrySendInGameICMessage(uid, CensorMessage(comp), InGameICChatType.Speak, ChatTransmitRange.Normal, true);

                comp.NextTimeCanSpeak += comp.SpeechCooldown;
            }

            // If the log changes when you're in cooldown, you still want to update the log so it won't trigger immediately
            comp.LastLog = log;
        }
    }

    private void OnTransformSpeech(Entity<SpeakOnExceptionComponent> ent, ref TransformSpeechEvent args)
    {
        if (_random.Prob(ent.Comp.ChanceSpeakNoAccent))
            args.Cancel();
    }

    private string CensorMessage(SpeakOnExceptionComponent comp)
    {
        return Loc.GetString(_random.Pick(_proto.Index(comp.Dataset).Values));
    }

    public override void Shutdown()
    {
        _log.RootSawmill.RemoveHandler(_logHandler);
    }
}

// Log handler for SpeakOnException entities.
public sealed class SpeakOnExceptionLogHandler : ILogHandler
{
    // Last error log that occured
    public string? LastLog;

    public void Log(string sawmillName, LogEvent message)
    {
        if (message.Exception == null)
            return;

        LastLog = message.Exception.Message;
    }
}
