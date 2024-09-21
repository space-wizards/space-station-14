limited-charges-charges-remaining =
    Имеется { $charges } { $charges ->
        [one] заряд
        [few] заряда
       *[other] зарядов
    }
limited-charges-max-charges = Имеет [color=green]максимум[/color] зарядов.
limited-charges-recharging =
    { $seconds ->
        [one] До нового заряда осталась [color=yellow]{ $seconds }[/color] секунда.
        [few] До нового заряда осталось [color=yellow]{ $seconds }[/color] секунды.
       *[other] До нового заряда осталось [color=yellow]{ $seconds }[/color] секунд.
    }
