// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.Chat;
using Content.Shared.DeadSpace.QueueTerminal;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.QueueTerminal;

/// <summary>
/// Серверная логика для <see cref="QueueTerminalComponent"/>: выдача билетов,
/// реагирование на связанный сигнал (любая кнопка, подключенная через систему связи сигналов /
/// систему связи устройств) для вызова и последующего обслуживания/уничтожения билетов, и
/// поддержание согласованности очереди, если вызванный или ожидающий билет исчезает
/// преждевременно (отброшен, сожжен, удален и т.д.)
/// </summary>
public sealed class QueueTerminalSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<QueueTerminalComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<QueueTerminalComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<QueueTicketComponent, ComponentShutdown>(OnTicketShutdown);
    }

    /// <summary>
    /// Игрок взаимодействует с терминалом напрямую -> берет новый билет,
    /// если у него еще нет незавершенного билета с этого терминала.
    /// Логика сервера работает в однопоточной очереди событий, поэтому два
    /// одновременных взаимодействия «взять билет» обрабатываются по очереди
    /// - NextNumber никогда не может быть выдан дважды
    /// </summary>
    private void OnActivate(EntityUid uid, QueueTerminalComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!IsPowered(uid))
        {
            _popup.PopupEntity(Loc.GetString("queue-terminal-no-power"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        if (comp.IssuedTo.Contains(args.User))
        {
            _popup.PopupEntity(Loc.GetString("queue-terminal-already-have-ticket"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!TryTakeNextNumber((uid, comp), out var number))
        {
            _popup.PopupEntity(Loc.GetString("queue-terminal-full"), uid, args.User);
            args.Handled = true;
            return;
        }

        var ticket = Spawn(comp.TicketPrototype, Transform(uid).Coordinates);
        var ticketComp = EnsureComp<QueueTicketComponent>(ticket);
        ticketComp.Number = number;
        ticketComp.Terminal = uid;
        ticketComp.TicketOwner = args.User;
        Dirty(ticket, ticketComp);

        UpdateTicketAppearance(ticket, ticketComp);

        comp.PendingTickets.Enqueue(ticket);
        comp.IssuedTo.Add(args.User);

        _audio.PlayPvs(comp.TicketPrintSound, uid);
        _hands.TryPickupAnyHand(args.User, ticket, handsComp: hands);

        args.Handled = true;
    }

    /// <summary>
    /// Срабатывает всякий раз, когда сигнал поступает на порт, к которому подключен этот объект.
    /// (система привязки устройств/сигнальных устройств — работает с любой кнопкой сигнала
    /// или другим источником сигнала в игре, подключенным обычным способом). Короткая
    /// задержка компенсирует случайные двойные нажатия/дублирование сигналов, поэтому
    /// одиночное нажатие никогда не сможет незаметно пропустить билет в очереди
    /// </summary>
    private void OnSignalReceived(Entity<QueueTerminalComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.CallPort)
            return;

        if (!IsPowered(ent))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextSignalTime)
            return;

        ent.Comp.NextSignalTime = now + ent.Comp.SignalCooldown;

        if (ent.Comp.CalledTicket == null)
            CallNext(ent);
        else
            ServeCurrent(ent);
    }

    /// <summary>
    /// Извлекает следующий действительный билет из очереди ожидания и вызывает его:
    /// обновляет дисплей, воспроизводит звук вызова и заставляет терминал
    /// произносить номер вслух
    /// молча пропускает (и забывает) любые билеты в очереди,
    /// которые больше не существуют, например, потому что они были сожжены или утеряны до
    /// вызова.
    /// </summary>
    private void CallNext(Entity<QueueTerminalComponent> ent)
    {
        var comp = ent.Comp;
        EntityUid? next = null;

        while (comp.PendingTickets.TryDequeue(out var candidate))
        {
            if (Exists(candidate) && TryComp<QueueTicketComponent>(candidate, out _))
            {
                next = candidate;
                break;
            }
        }

        if (next == null)
        {
            _popup.PopupEntity(Loc.GetString("queue-terminal-empty"), ent);
            return;
        }

        var number = CompOrNull<QueueTicketComponent>(next.Value)?.Number ?? 0;

        comp.CalledTicket = next;
        comp.CalledNumber = number;
        Dirty(ent, comp);

        UpdateTerminalAppearance(ent, comp);
        _audio.PlayPvs(comp.CallSound, ent);

        var message = Loc.GetString("queue-terminal-calling", ("number", FormatNumber(number)));
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);
    }

    /// <summary>
    /// Второе нажатие для текущего вызываемого билета: посетитель обслужен,
    /// поэтому билет уничтожается , и терминал возвращается в режим ожидания, готовый к следующему
    /// нажатию для вызова следующего номера.
    /// </summary>
    private void ServeCurrent(Entity<QueueTerminalComponent> ent)
    {
        var comp = ent.Comp;
        var called = comp.CalledTicket;

        comp.CalledTicket = null;
        comp.CalledNumber = 0;
        Dirty(ent, comp);
        UpdateTerminalAppearance(ent, comp);

        if (called != null && Exists(called.Value))
        {
        // Всплывающее окно отображается тому, кто непосредственно держит билет;
        // PopupEntity корректно не выполняет никаких действий, если на другом конце нет прикрепленной сессии игрока (например, билет лежит на полу, а не находится в чьих-то руках).
            var holder = Transform(called.Value).ParentUid;
            _popup.PopupEntity(Loc.GetString("queue-ticket-burn"), called.Value, holder);

            QueueDel(called.Value);
        }
    }

    /// <summary>
    /// Если сущность билета удалена, терминал
    /// освободит слот "один билет за раз" для его владельца на терминале, выдающем билет, и если он все еще
    /// является "текущим" билетом терминала, очистит и эту ссылку,
    /// чтобы терминал не думал, что он ожидает обслуживания билета, которого больше нет
    /// </summary>
    private void OnTicketShutdown(Entity<QueueTicketComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Terminal is not { } terminal || !TryComp<QueueTerminalComponent>(terminal, out var termComp))
            return;

        if (ent.Comp.TicketOwner is { } owner)
            termComp.IssuedTo.Remove(owner);

        if (termComp.CalledTicket == ent.Owner)
        {
            termComp.CalledTicket = null;
            termComp.CalledNumber = 0;
            Dirty(terminal, termComp);
            UpdateTerminalAppearance(terminal, termComp);
        }
    }

    private void UpdateTerminalAppearance(EntityUid uid, QueueTerminalComponent comp)
    {
        _appearance.SetData(uid, QueueDisplayVisuals.Number, comp.CalledNumber);
    }

    private void UpdateTicketAppearance(EntityUid uid, QueueTicketComponent comp)
    {
        _appearance.SetData(uid, QueueDisplayVisuals.Number, comp.Number);
    }

    private static string FormatNumber(int number)
    {
        return number.ToString("D3");
    }

    private bool IsPowered(EntityUid uid)
    {
        return !TryComp<ApcPowerReceiverComponent>(uid, out var receiver) || receiver.Powered;
    }

    private bool TryTakeNextNumber(Entity<QueueTerminalComponent> ent, out int number)
    {
        var reserved = new HashSet<int>();

        if (ent.Comp.CalledNumber is >= 1 and <= QueueTerminalComponent.MaxNumber)
            reserved.Add(ent.Comp.CalledNumber);

        foreach (var ticket in ent.Comp.PendingTickets)
        {
            if (TryComp<QueueTicketComponent>(ticket, out var ticketComp) &&
                ticketComp.Number is >= 1 and <= QueueTerminalComponent.MaxNumber)
            {
                reserved.Add(ticketComp.Number);
            }
        }

        var candidate = ent.Comp.NextNumber;
        if (candidate is < 1 or > QueueTerminalComponent.MaxNumber)
            candidate = 1;

        for (var i = 0; i < QueueTerminalComponent.MaxNumber; i++)
        {
            if (!reserved.Contains(candidate))
            {
                number = candidate;
                ent.Comp.NextNumber = candidate == QueueTerminalComponent.MaxNumber ? 1 : candidate + 1;
                return true;
            }

            candidate = candidate == QueueTerminalComponent.MaxNumber ? 1 : candidate + 1;
        }

        number = default;
        return false;
    }
}
