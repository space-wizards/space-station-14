limited-charges-charges-remaining =
    { $charges ->
        [one] Остался [color=fuchsia]{ $charges }[/color] заряд.
        [few] Осталось [color=fuchsia]{ $charges }[/color] заряда.
       *[other] Осталось [color=fuchsia]{ $charges }[/color] зарядов.
    }
limited-charges-max-charges = Имеется [color=green]максимум[/color] зарядов.
limited-charges-recharging =
    { $seconds ->
        [one] До следующего заряда осталась [color=yellow]{ $seconds }[/color] секунда.
        [few] До следующего заряда осталось [color=yellow]{ $seconds }[/color] секунды.
       *[other] До следующего заряда осталось [color=yellow]{ $seconds }[/color] секунд.
    }
