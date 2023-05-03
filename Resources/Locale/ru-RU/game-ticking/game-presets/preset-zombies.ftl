zombie-title = Зомби
zombie-description = На станции появился вирус, способный оживлять мертвецов! Совместно с другими членами экипажа сдержите заражение и постарайтесь выжить.
zombie-not-enough-ready-players = Недостаточно игроков готовы к игре! { $readyPlayersCount } игроков из необходимых { $minimumPlayers } готовы. Нельзя начать Зомби.
zombie-no-one-ready = Нет готовых игроков! Нельзя начать Зомби.
zombie-patientzero-role-greeting = Вы — нулевой пациент. Скрывайте свою инфекцию, добывайте припасы, и будьте готовы обратиться после смерти.
zombie-alone = Вы чувствуете себя совершенно одиноким.
zombie-round-end-initial-count =
    { $initialCount ->
        [one] Единственным нулевым пациентом был:
       *[other] Нулевых пациентов было { $initialCount }, ими были:
    }
zombie-round-end-user-was-initial = - [color=plum]{ $name }[/color] ([color=gray]{ $username }[/color]) был одним из нулевых пациентов.
zombie-round-end-amount-none = [color=green]Все зомби были уничтожены![/color]
zombie-round-end-amount-low = [color=green]Почти все зомби были уничтожены.[/color]
zombie-round-end-amount-medium = [color=yellow]{ $percent }% экипажа были обращены в зомби.[/color]
zombie-round-end-amount-high = [color=crimson]{ $percent }% экипажа были обращены в зомби.[/color]
zombie-round-end-amount-all = [color=darkred]Весь экипаж обратился в зомби![/color]
zombie-round-end-survivor-count =
    { $count ->
        [one] Единственным выжившим стал:
       *[other] Осталось всего { $count } выживших, это:
    }
zombie-round-end-user-was-survivor = - [color=White]{ $name }[/color] ([color=gray]{ $username }[/color]) пережил заражение.
