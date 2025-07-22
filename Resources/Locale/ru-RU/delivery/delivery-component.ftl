delivery-recipient-examine = Адресовано: { $recipient }, { $job }.
delivery-already-opened-examine = Уже вскрыто.
delivery-earnings-examine = Delivering this will earn the station [color=yellow]{ $spesos }[/color] spesos.
delivery-recipient-no-name = Безымянный
delivery-recipient-no-job = Неизвестно
delivery-unlocked-self = Вы разблокировали { $delivery } отпечатком пальца.
delivery-opened-self = Вы вскрываете { $delivery }.
delivery-unlocked-others =
    { CAPITALIZE($recipient) } { GENDER($recipient) ->
        [male] разблокировал
        [female] разблокировала
        [epicene] разблокировали
       *[neuter] разблокировало
    } { $delivery } используя свой отпечаток пальца.
delivery-opened-others =
    { CAPITALIZE($recipient) } { GENDER($recipient) ->
        [male] вскрыл
        [female] вскрыл
        [epicene] вскрыл
       *[neuter] вскрыл
    } { $delivery }.
delivery-unlock-verb = Разблокировать
delivery-open-verb = Вскрыть
delivery-slice-verb = Slice open
delivery-teleporter-amount-examine =
    { $amount ->
        [one] It contains [color=yellow]{ $amount }[/color] delivery.
       *[other] It contains [color=yellow]{ $amount }[/color] deliveries.
    }
delivery-teleporter-empty = The { $entity } is empty.
delivery-teleporter-empty-verb = Take mail
# modifiers
delivery-priority-examine = This is a [color=orange]priority { $type }[/color]. You have [color=orange]{ $time }[/color] left to deliver it to get a bonus.
delivery-priority-delivered-examine = This is a [color=orange]priority { $type }[/color]. It got delivered on time.
delivery-priority-expired-examine = This is a [color=orange]priority { $type }[/color]. It ran out of time.
delivery-fragile-examine = This is a [color=red]fragile { $type }[/color]. Deliver it intact for a bonus.
delivery-fragile-broken-examine = This is a [color=red]fragile { $type }[/color]. It looks badly damaged.
delivery-bomb-examine = This is a [color=purple]bomb { $type }[/color]. Oh no.
delivery-bomb-primed-examine = This is a [color=purple]bomb { $type }[/color]. Reading this is a bad use of your time.
