delivery-recipient-examine = This one is meant for {$recipient}, {$job}.
delivery-already-opened-examine = It was already opened.
delivery-earnings-examine = Delivering this will earn the station [color=yellow]{$spesos}[/color] spesos.
delivery-recipient-no-name = Unnamed
delivery-recipient-no-job = Unknown

delivery-unlocked-self = You unlock the {$delivery} with your fingerprint.
delivery-opened-self = You open the {$delivery}.
delivery-unlocked-others = {CAPITALIZE($recipient)} unlocked the {$delivery} with {POSS-ADJ($possadj)} fingerprint.
delivery-opened-others = {CAPITALIZE($recipient)} opened the {$delivery}.

delivery-unlock-verb = Unlock
delivery-open-verb = Open
delivery-slice-verb = Slice open

delivery-teleporter-amount-examine =
    { $amount ->
        [one] It contains [color=yellow]{$amount}[/color] delivery.
        *[other] It contains [color=yellow]{$amount}[/color] deliveries.
    }
delivery-teleporter-empty = The {$entity} is empty.
delivery-teleporter-empty-verb = Take mail


# modifiers
delivery-priority-examine = This is a [color=orange]priority {$type}[/color]. You have [color=orange]{$time}[/color] left to deliver it to get a bonus.
delivery-priority-delivered-examine = This is a [color=orange]priority {$type}[/color]. It got delivered on time.
delivery-priority-expired-examine = This is a [color=orange]priority {$type}[/color]. It ran out of time.

delivery-fragile-examine = This is a [color=red]fragile {$type}[/color]. Deliver it intact for a bonus.
delivery-fragile-broken-examine = This is a [color=red]fragile {$type}[/color]. It looks badly damaged.

delivery-bomb-examine = This is a [color=purple]bomb {$type}[/color]. Oh no.
delivery-bomb-primed-examine = This is a [color=purple]bomb {$type}[/color]. Reading this is a bad use of your time.
