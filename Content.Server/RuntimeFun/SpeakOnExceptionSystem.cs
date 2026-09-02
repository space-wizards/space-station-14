using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.Server.RuntimeFun;

/// <summary>
///     System for the <see cref="SpeakOnExceptionComponent"/>. Deals with getting the latest error log and making
///     entities with that component speak.
/// </summary>
public sealed partial class SpeakOnExceptionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;

    // Special log handler that just saves the latest error.
    private SpeakOnExceptionLogHandler _logHandler = default!;

    public override void Initialize()
    {
        base.Initialize();

        _logHandler = new SpeakOnExceptionLogHandler();
        _log.RootSawmill.AddHandler(_logHandler);

        SubscribeLocalEvent<SpeakOnExceptionComponent, TransformSpeechEvent>(OnTransformSpeech, before: [ typeof(AccentSystem) ]);
    }

    public override void Shutdown()
    {
        _log.RootSawmill.RemoveHandler(_logHandler);
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

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SpeakOnExceptionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextTimeCanSpeak = _timing.CurTime;
    }

    private void OnTransformSpeech(Entity<SpeakOnExceptionComponent> ent, ref TransformSpeechEvent args)
    {
        if (ent.Comp.BlockAccent)
            args.Cancel();
    }

    private string CensorMessage(SpeakOnExceptionComponent comp)
    {
        return Loc.GetString(_random.Pick(ProtoMan.Index(comp.Dataset).Values));
    }

    // Log handler for SpeakOnException entities.
    private sealed class SpeakOnExceptionLogHandler : ILogHandler
    {
        // Gets set to true if an error ever occurs - reset this to false if you want to see if another error has occurred!
        public bool ErrorHasOccured;

        public void Log(string sawmillName, LogEvent message)
        {
            if (message.Exception == null)
                return;

            ErrorHasOccured = true;
        }
    }
}

