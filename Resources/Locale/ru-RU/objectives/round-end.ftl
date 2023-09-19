objectives-round-end-result =
    { $count ->
        [one] Был один { $agent }.
        [few] Было { $count } { $agent }.
       *[other] Было { $count } { $agent }.
    }
objectives-player-user-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color])
objectives-player-user = [color=gray]{ $user }[/color]
objectives-player-named = [color=White]{ $name }[/color]
objectives-no-objectives = { $title } – { $agent }.
objectives-with-objectives = { $title } – { $agent } со следующими целями:
objectives-objective-success = { $objective } | [color={ $markupColor }]Успех![/color]
objectives-objective-fail = { $objective } | [color={ $markupColor }]Провал![/color] ({ $progress }%)
