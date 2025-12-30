limited-charges-charges-remaining =
    Имеется { $charges } { $charges ->
        [one] заряд
        [few] заряда
       *[other] зарядов
    }.
limited-charges-max-charges = Имеет [color=green]максимум[/color] зарядов.
limited-charges-recharging =
    До нового заряда { $seconds ->
        [one] осталась [color=yellow]{ $seconds }[/color] секунда.
        [few] осталось [color=yellow]{ $seconds }[/color] секунды.
       *[other] осталось [color=yellow]{ $seconds }[/color] секунд.
    }
