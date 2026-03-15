using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.Server.RuntimeFun;

/// <summary>
///     System for the <see cref="SpeakOnExceptionComponent"/>. Deals with getting the latest error log and making
///     entities with that component speak.
/// </summary>
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
    }

    public override void Update(float frameTime)
    {
        if (!_logHandler.ErrorHasOccured)
            return;

        _logHandler.ErrorHasOccured = false;

        var query = EntityQueryEnumerator<SpeakOnExceptionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextTimeCanSpeak)
                continue;

            if (_random.Prob(comp.ChanceSpeakNoAccent))
                comp.BlockAccent = true;

            _chat.TrySendInGameICMessage(uid, CensorMessage(comp), InGameICChatType.Speak, ChatTransmitRange.Normal, true);

            comp.BlockAccent = false;

            comp.NextTimeCanSpeak += comp.SpeechCooldown;
        }
    }

    private void OnTransformSpeech(Entity<SpeakOnExceptionComponent> ent, ref TransformSpeechEvent args)
    {
        if (ent.Comp.BlockAccent)
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

    // Log handler for SpeakOnException entities.
    private sealed class SpeakOnExceptionLogHandler : ILogHandler
    {
        // Gets set to true if an error ever occurs - reset this too false if you want to see if another error occured!
        public bool ErrorHasOccured;

        public void Log(string sawmillName, LogEvent message)
        {
            if (message.Exception == null)
                return;

            ErrorHasOccured = true;
        }
    }
}

