plant-analyzer-component-no-seed = no plant found

plant-analyzer-component-health = Health:
plant-analyzer-component-age = Age:
plant-analyzer-component-water = Water:
plant-analyzer-component-nutrition = Nutrition:
plant-analyzer-component-toxins = Toxins:
plant-analyzer-component-pests = Pests:
plant-analyzer-component-weeds = Weeds:

plant-analyzer-component-alive = [color=green]ALIVE[color]
plant-analyzer-component-dead = [color=red]DEAD[color]
plant-analyzer-component-unviable = [color=red]UNVIABLE[color]
plant-analyzer-component-mutating = [color=#00ff5f]MUTATING[color]
plant-analyzer-component-kudzu = [color=red]KUDZU[color]

plant-analyzer-soil = There is some [color=white]{$chemicals}[/color] in this {$holder} that {$count ->
    [one]has
    *[other]have
} not been absorbed.
plant-analyzer-soil-empty = There are no unabsorbed chemicals in this {$holder}.

plant-analyzer-component-environemt = This [color=green]{$seedName}[/color] requires an atmosphere at a pressure level of [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color], temperature of [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color] and a light level of [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-void = This [color=green]{$seedName}[/color] has to be grown [bolditalic]in the vacuum of space[/bolditalic] at a light level of [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-gas = This [color=green]{$seedName}[/color] requires an atmosphere containing [bold]{$gases}[/bold] at a pressure level of [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color], temperature of [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color] and a light level of [color=white]{$lightLevel} ± {$lightTolerance}[/color].

plant-analyzer-produce-plural = {MAKEPLURAL($thing)}
plant-analyzer-output = {$yield ->
    [0]{$gasCount ->
        [0]The only thing it seems to do is consume water and nutrients.
        *[other]The only thing it seems to do is turn water and nutrients into [bold]{$gases}[/bold].
    }
    *[other]It has [color=lightgreen]{$yield} {$potency}[/color]{$seedless ->
        [true]{" "}but [color=red]seedless[/color]
        *[false]{$nothing}
    }{" "}{$yield ->
        [one]flower
        *[other]flowers
    }{" "}that{$gasCount ->
        [0]{$nothing}
        *[other]{$yield ->
            [one]{" "}emits
            *[other]{" "}emit
        }{" "}[bold]{$gases}[/bold] and
    }{" "}will turn into{$yield ->
        [one]{" "}{INDEFINITE($firstProduce)} [color=#a4885c]{$produce}[/color]
        *[other]{" "}[color=#a4885c]{$producePlural}[/color]
    }.{$chemCount ->
        [0]{$nothing}
        *[other]{" "}There are trace amounts of [color=white]{$chemicals}[/color] in its stem.
    }
}

plant-analyzer-potency-tiny = tiny
plant-analyzer-potency-small = small
plant-analyzer-potency-below-average = below-average sized
plant-analyzer-potency-average = average sized
plant-analyzer-potency-above-average = above-average sized
plant-analyzer-potency-large = rather large
plant-analyzer-potency-huge = huge
plant-analyzer-potency-gigantic = gigantic
plant-analyzer-potency-ludicrous = ludicrously large
plant-analyzer-potency-immeasurable = immeasurably large

plant-analyzer-print = Print
plant-analyzer-printout-missing = N/A
plant-analyzer-printout = [color=#9FED58][head=2]Plant Analyzer Report[/head][/color]{$nl
    }──────────────────────────────{$nl
    }[bullet/] Species: {$seedName}{$nl
    }{$indent}[bullet/] Viable: {$viable ->
        [no][color=red]No[/color]
        [yes][color=green]Yes[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }{$nl
    }{$indent}[bullet/] Endurance: {$endurance}{$nl
    }{$indent}[bullet/] Lifespan: {$lifespan}{$nl
    }{$indent}[bullet/] Product: [color=#a4885c]{$produce}[/color]{$nl
    }{$indent}[bullet/] Kudzu: {$kudzu ->
        [no][color=green]No[/color]
        [yes][color=red]Yes[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }{$nl
    }[bullet/] Growth profile:{$nl
    }{$indent}[bullet/] Water: [color=cyan]{$water}[/color]{$nl
    }{$indent}[bullet/] Nutrition: [color=orange]{$nutrients}[/color]{$nl
    }{$indent}[bullet/] Toxins: [color=yellowgreen]{$toxins}[/color]{$nl
    }{$indent}[bullet/] Pests: [color=magenta]{$pests}[/color]{$nl
    }{$indent}[bullet/] Weeds: [color=red]{$weeds}[/color]{$nl
    }[bullet/] Environmental profile:{$nl
    }{$indent}[bullet/] Composition: [bold]{$gasesIn}[/bold]{$nl
    }{$indent}[bullet/] Pressure: [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color]{$nl
    }{$indent}[bullet/] Temperature: [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color]{$nl
    }{$indent}[bullet/] Light: [color=gray][bold]{$lightLevel} ± {$lightTolerance}[/bold][/color]{$nl
    }[bullet/] Flowers: {$yield ->
        [-1]{LOC("plant-analyzer-printout-missing")}
        [0][color=red]0[/color]
        *[other][color=lightgreen]{$yield} {$potency}[/color]
    }{$nl
    }[bullet/] Seeds: {$seeds ->
        [no][color=red]No[/color]
        [yes][color=green]Yes[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }{$nl
    }[bullet/] Chemicals: [color=gray][bold]{$chemicals}[/bold][/color]{$nl
    }[bullet/] Emissions: [bold]{$gasesOut}[/bold]
