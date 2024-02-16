objectives-round-end-result = {$count ->
    [one] There was one {$agent}.
    *[other] There were {$count} {MAKEPLURAL($agent)}.
}

objectives-round-end-result-in-custody = {$custody} out of {$count} {MAKEPLURAL($agent)} were in custody.

objectives-player-user-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color])
objectives-player-user = [color=gray]{$user}[/color]
objectives-player-named = [color=White]{$name}[/color]

objectives-no-objectives = {$custody}{$title} was a {$agent}.
objectives-with-objectives = {$custody}{$title} was a {$agent} who had the following objectives:

objectives-objective-success = {$objective} | [color={$markupColor}]Success![/color]
objectives-objective-fail = {$objective} | [color={$markupColor}]Failure![/color] ({$progress}%)

objectives-in-custody = [bold][color=red]| IN CUSTODY | [/color][/bold]
