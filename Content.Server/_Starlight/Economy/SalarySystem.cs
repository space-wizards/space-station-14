using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Stack;
using Content.Server.Starlight;
using Content.Server.Station.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.UserInterface;
using JetBrains.FormatRipper.Elf;
using NAudio.CoreAudioApi;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Starlight.Economy;
public sealed partial class SalarySystem : SharedSalarySystem
{
    [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private float _delayAccumulator = 0f;
    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<ICommonSession, TimeSpan> _lastSalary = [];
    private SalariesPrototype _salaries = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(ev => _lastSalary.Clear());
        _salaries = _prototypes.Index<SalariesPrototype>("standart");
        base.Initialize();
    }
    public override void Update(float frameTime)
    {
        _delayAccumulator += frameTime;
        if (_delayAccumulator > 2)
        {
            _delayAccumulator = 0;
            _stopwatch.Restart();

            var query = _playerRolesManager.Players.GetEnumerator();
            while (query.MoveNext() && query.Current != null && _stopwatch.Elapsed < TimeSpan.FromMilliseconds(0.1))
            {
                if (!_lastSalary.TryGetValue(query.Current.Session, out var lastTime))
                {
                    _lastSalary.Add(query.Current.Session, _time.CurTime);
                    continue;
                }
                if (_time.CurTime - lastTime > TimeSpan.FromMinutes(15)
                    && _mind.TryGetMind(query.Current.Session.UserId, out var mind))
                {

                    var roles = _roles.MindGetAllRoleInfo((mind.Value.Owner, mind.Value.Comp));
                    foreach (var role in roles)
                    {
                        if (_salaries.Jobs.TryGetValue(role.Prototype, out var salary))
                        {
                            var amount = CalculateSalaryWithBonuses(salary, query.Current.Data.Flags);

                            query.Current.Data.Balance += amount;
                            var message = Loc.GetString("economy-chat-salary-message", ("amount", amount), ("sender", "NanoTrasen"));
                            var wrappedMessage = Loc.GetString("economy-chat-salary-wrapped-message", ("amount", amount), ("sender", "NanoTrasen"), ("senderColor", "#2384CE"));
                            _chat.ChatMessageToOne(ChatChannel.Notifications, message, wrappedMessage, default, false, query.Current.Session.Channel, Color.FromHex("#57A3F7"));
                        }
                    }

                    _lastSalary[query.Current.Session] = _time.CurTime;
                }
            }
        }
    }

    private int CalculateSalaryWithBonuses(int baseSalary, PlayerFlags flags)
    {
        var bonusMultiplier = 1.0;

        if (flags.HasFlag(PlayerFlags.Staff))
            bonusMultiplier += 0.5;

        if (flags.HasFlag(PlayerFlags.Mentor))
            bonusMultiplier += 0.45;

        if (flags.HasFlag(PlayerFlags.Retiree))
            bonusMultiplier += 0.30;

        if (flags.HasFlag(PlayerFlags.AlfaTester))
            bonusMultiplier += 0.30;

        if (flags.HasFlag(PlayerFlags.BetaTester))
            bonusMultiplier += 0.15;

        if (flags.HasFlag(PlayerFlags.CopperEventWinner))
            bonusMultiplier += 0.10;

        if (flags.HasFlag(PlayerFlags.SilverEventWinner))
            bonusMultiplier += 0.20;

        if (flags.HasFlag(PlayerFlags.GoldEventWinner))
            bonusMultiplier += 0.30;

        return (int)Math.Ceiling(baseSalary * bonusMultiplier);
    }
}