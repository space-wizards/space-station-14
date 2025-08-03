using System.Linq;
using Content.Server._NullLink.PlayerData;
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
using Content.Shared._NullLink;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.UserInterface;
using NAudio.CoreAudioApi;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Content.Shared.Starlight.Economy;
public sealed partial class SalarySystem : SharedSalarySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly INullLinkPlayerManager _nullLinkRoles = default!;
    [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
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
                if (!_entityManager.TryGetComponent<MobStateComponent>(query.Current.Session.AttachedEntity, out var state) 
                    || state.CurrentState == MobState.Critical 
                    || state.CurrentState == MobState.Dead)
                    continue;
                if (_time.CurTime - lastTime > TimeSpan.FromMinutes(15)
                    && _mind.TryGetMind(query.Current.Session.UserId, out var mind))
                {

                    var roles = _roles.MindGetAllRoleInfo((mind.Value.Owner, mind.Value.Comp));
                    foreach (var role in roles)
                    {
                        if (_salaries.Jobs.TryGetValue(role.Prototype, out var salary))
                        {
                            var amount = CalculateSalaryWithBonuses(salary, query.Current.Session);

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

    private int CalculateSalaryWithBonuses(int baseSalary, ICommonSession session)
    {
        var bonusMultiplier = 1.0;

        if (!_nullLinkRoles.TryGetPlayerData(session.UserId, out var playerData))
            return baseSalary;

        foreach (var bonus in _prototypes.EnumeratePrototypes<SalaryRoleBonusPrototype>())
            if(bonus.Roles.Any(playerData.Roles.Contains))
                bonusMultiplier += bonus.Multiplayer;

        return (int)Math.Ceiling(baseSalary * bonusMultiplier);
    }

    internal void Donate(ICommonSession session, int amount)
    {
        var playerData = _playerRolesManager.GetPlayerData(session);
        if (playerData == null)
            return;

        playerData.Balance += amount;

        // We need to make a prototype
        var i = _random.Next(0, 20);
        var message = Loc.GetString($"economy-chat-donate-{i}-message", ("amount", amount));
        var wrappedMessage = Loc.GetString($"economy-chat-donate-{i}-wrapped-message", ("amount", amount));
        _chat.ChatMessageToOne(ChatChannel.Notifications, message, wrappedMessage, default, false, session.Channel, Color.FromHex("#57A3F7"));
    }
}