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
guidebook-reagent-recipes-reagent-display = [bold]{$reagent}[/bold] \[{$ratio}\]
guidebook-reagent-recipes-mix = Mix
guidebook-reagent-effects-header = Effects
guidebook-reagent-effects-metabolism-group-rate = [bold]{$group}[/bold] [color=gray]({$rate} units per second)[/color]
guidebook-reagent-physical-description = [italic]Seems to be {$description}.[/italic]
guidebook-reagent-recipes-mix-info = {$minTemp ->
    [0] {$hasMax ->
            [true] {$verb} below {$maxTemp}K
            *[false] {$verb}
        }
    *[other] {$verb} {$hasMax ->
            [true] between {$minTemp}K and {$maxTemp}K
            *[false] above {$minTemp}K
        }
}
