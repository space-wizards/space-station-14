objectives-round-end-result = {$count ->
    [one] Había un {$agent}.
    *[other] Había {$count} {MAKEPLURAL($agent)}.
}

objectives-round-end-result-in-custody = {$custody} de {$count} {MAKEPLURAL($agent)} estaban bajo custodia.

objectives-player-user-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color])
objectives-player-named = [color=White]{$name}[/color]

objectives-no-objectives = {$custody}{$title} era un {$agent}.
objectives-with-objectives = {$custody}{$title} era un {$agent} que tenía los siguientes objetivos:

objectives-objective-success = {$objective} | [color={$markupColor}]¡Éxito![/color]
objectives-objective-fail = {$objective} | [color={$markupColor}]¡Fracaso![/color] ({$progress}%)

objectives-in-custody = [bold][color=red]| EN CUSTODIA | [/color][/bold]
