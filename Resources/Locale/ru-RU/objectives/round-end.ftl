objectives-round-end-result = {$count ->
    [one] Был один {$agent}.
    *[other] Было {$count} {MAKEPLURAL($agent)}.
}

objectives-player-user-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color])
objectives-player-user = [color=gray]{$user}[/color]
objectives-player-named = [color=White]{$name}[/color]

objectives-no-objectives = {$title} был {$agent}.
objectives-with-objectives = {$title} был {$agent} со следующими задачами:

objectives-objective-success = {$objective} | [color={$markupColor}]Успех![/color]
objectives-objective-fail = {$objective} | [color={$markupColor}]Неудача![/color] ({$progress}%)
