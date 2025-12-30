point-scoreboard-winner = Победитель — [color=lime]{ $player }![/color]
point-scoreboard-header = [bold]Таблица результатов[/bold]
point-scoreboard-list =
    { $place }. [bold][color=cyan]{ $name }[/color][/bold] набирает [color=yellow]{ $points ->
        [one] { $points } очко
       *[other] { $points } очков
    }.[/color]
