guidebook-reagent-effect-description =
    {$chance ->
        [1] { $effect }
        *[other] Has a { NATURALPERCENT($chance, 2) } chance to { $effect }
    }{ $conditionCount ->
        [0] .
        *[other] {" "}when { $conditions }.
    }

guidebook-reagent-name = [bold][color={$color}]{CAPITALIZE($name)}[/color][/bold]
guidebook-reagent-recipes-header = Recipe
guidebook-reagent-effects-header = Effects
guidebook-reagent-effects-metabolism-group-rate = [bold]{$group}[/bold] [color=gray]({$rate} units per second)[/color]
guidebook-reagent-physical-description = Seems to be {$description}.

guidebook-reaction-prototype-display = [bold]{$name}[/bold] \[{$ratio}\]
guidebook-catalyst-prototype-display = [bolditalic]{$name}[/bolditalic] \[{$ratio}\]
guidebook-reaction-mix = Mix
guidebook-reaction-mix-and-heat = Mix at above {$temperature}K