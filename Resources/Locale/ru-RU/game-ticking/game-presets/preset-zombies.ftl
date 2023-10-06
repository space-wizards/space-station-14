zombie-title = Зомби
zombie-description = На станции появились ожившие мертвецы! Работайте сообща с другими членами экипажа, чтобы пережить эпидемию и защитить станцию.
zombie-not-enough-ready-players = Недостаточно игроков готовы к игре! { $readyPlayersCount } игроков из необходимых { $minimumPlayers } готовы. Нельзя запустить пресет Зомби.
zombie-no-one-ready = Нет готовых игроков! Нельзя запустить пресет Зомби.
zombie-patientzero-role-greeting = Вы — нулевой пациент. Снарядитесь и подготовьтесь к своему превращению. Ваша цель - захватить станцию, заразив при этом как можно больше членов экипажа.
zombie-healing = Вы ощущаете шевеление в своей плоти
zombie-infection-warning = Вы чувствуете, как зомби-вирус берёт верх
zombie-infection-underway = Ваша кровь начинает сгущаться
zombie-alone = Вы чувствуете себя совершенно одиноким.
zombie-shuttle-call = Мы зафиксировали, что зомби захватили станцию. Аварийный шаттл был отправлен для эвакуации оставшегося персонала.
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
