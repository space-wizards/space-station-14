objectives-round-end-result =
    { $count ->
        [one] Был один { $agent }.
        [few] Было { $count } { $agent }.
       *[other] Было { $count } { $agent }.
    }
objectives-round-end-result-in-custody = { $custody } из { $count } { $agent } были арестованы.
objectives-player-user-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color])
objectives-player-named = [color=White]{ $name }[/color]
objectives-no-objectives = { $custody }{ $title } – { $agent }.
objectives-with-objectives = { $custody }{ $title } – { $agent } со следующими целями:
objectives-objective-success = { $objective } | [color=green]Успех![/color] ({ TOSTRING($progress, "P0") })
objectives-objective-partial-success = { $objective } | [color=yellow]Частичный успех![/color] ({ TOSTRING($progress, "P0") })
objectives-objective-partial-failure = { $objective } | [color=orange]Частичный провал![/color] ({ TOSTRING($progress, "P0") })
objectives-objective-fail = { $objective } | [color=red]Провал![/color] ({ TOSTRING($progress, "P0") })
objectives-in-custody = [bold][color=red]| АРЕСТОВАН | [/color][/bold]
