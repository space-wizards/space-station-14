using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // Special log handler that just saves the latest error.
    private SpeakOnExceptionLogHandler _logHandler = default!;

    private bool _censor;

    public override void Initialize()
    {
        base.Initialize();

        _logHandler = new SpeakOnExceptionLogHandler();
        _log.RootSawmill.AddHandler(_logHandler);

        SubscribeLocalEvent<SpeakOnExceptionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpeakOnExceptionComponent, TransformSpeechEvent>(OnTransformSpeech, before: [ typeof(AccentSystem) ]);

        Subs.CVar(_config, CCVars.CensorExceptionsInChat, x => _censor = x, true);
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
                if (_random.Prob(comp.ChanceSpeakNoAccent))
                    comp.BlockAccent = true;

                _chat.TrySendInGameICMessage(uid, TryCensorMessage(comp, log), InGameICChatType.Speak, ChatTransmitRange.Normal, true);
                comp.BlockAccent = false;

                comp.NextTimeCanSpeak += comp.SpeechCooldown;
            }

            // If the log changes when your in cooldown, you still want to update the log so it won't trigger immediately
            comp.LastLog = log;
        }
    }

    private void OnTransformSpeech(Entity<SpeakOnExceptionComponent> ent, ref TransformSpeechEvent args)
    {
        args.Cancelled |= ent.Comp.BlockAccent;
    }

    private string TryCensorMessage(SpeakOnExceptionComponent comp, string message)
    {
        return _censor ? Loc.GetString(_random.Pick(_proto.Index(comp.Dataset).Values)) : message;
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
