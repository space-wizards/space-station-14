using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// GameRule System которая перезапускает раунд через определенное время после его начала
/// </summary>
public sealed class AutoRoundRestartRuleSystem : GameRuleSystem<AutoRoundRestartRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private float _restartDelay = 180f; // время до рестарта
    private TimeSpan? _roundStartTime;
    private bool _roundStarted;
    private bool _notified;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("auto-restart");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged); //ивент смены уровня игры(InRound будет указана ниже).
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        Sawmill.Info($"[AutoRoundRestart] RunLevelChanged: {ev.New}"); //логирует смену стадии игры
        if (!EntityQuery<AutoRoundRestartRuleComponent>().Any())
            return;

        if (ev.New == GameRunLevel.InRound) // если раунд начался то есть GameRunLevel.InRound то таймер запускается.
        {
            OnRoundStart();
        }
        else // если стадия сменилась на любую другую (PostRound, PreRound, PreRoundLobby и т.д.) — сбрасываем таймер
        {
            if (_roundStarted)
            {
                Sawmill.Info("[AutoRoundRestart] RunLevel moved away from InRound, cancelling auto-restart timer.");
                ResetTimerState();
            }
        }
    }

    public override void Update(float frameTime) // вызывается каждый кадр
    {
        base.Update(frameTime);

        if (!_roundStarted || _roundStartTime == null) // если раунд не начался
            return;

        var timeSinceStart = _gameTiming.CurTime - _roundStartTime.Value; // время с начала рунда
        var secondsLeft = _restartDelay - (float)timeSinceStart.TotalSeconds; //время до рестарта

        // Уведомление на 30 секунд отключено (сообщения теперь задаются в прототипах других правил)
        if (!_notified && secondsLeft <= 30f && secondsLeft > 0f)
        {
            _notified = true;
        }

        if (timeSinceStart.TotalSeconds >= _restartDelay) // если время с начала раунда больше или равно времени до рестарта.
        {
            RestartRound();
            ResetTimerState();
        }
    }

    private void OnRoundStart() //вызывается когда  раунд стартуется
    {
        _roundStarted = true;
        _roundStartTime = _gameTiming.CurTime;
        _notified = false;
        // Стартовое сообщение отключено. Старт уведомлений и логов не отправляется.
    }

    private void NotifyPlayers(string message) //уведомление
    {
        // Disabled: announcements must be driven by prototype-based systems (AutoRoundEnding/Restart systems).
        // Intentionally no chat dispatch and no message echo here to avoid hardcoded phrases.
    }

    private void RestartRound() //отвечает за перезапуск раунда
    {
        _gameTicker.EndRound();
        Sawmill.Info("[AutoRoundRestart] changing InRound to PostRound. Using EndRound!");
    }
    private void ResetTimerState()
    {
        _roundStarted = false;
        _roundStartTime = null;
        _notified = false;
    }
}