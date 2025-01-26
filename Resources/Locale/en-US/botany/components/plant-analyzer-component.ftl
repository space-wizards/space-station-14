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
plant-analyzer-printout-l0 = [color=#9FED58][head=2]Plant Analyzer Report[/head][/color]
plant-analyzer-printout-l1 = ──────────────────────────────
plant-analyzer-printout-l2 = [bullet/] Species: {$seedName}
plant-analyzer-printout-l3 = {$indent}[bullet/] Viable: {$viable ->
    [no][color=red]No[/color]
    [yes][color=green]Yes[/color]
    *[other]{LOC("plant-analyzer-printout-missing")}
}
plant-analyzer-printout-l4 = {$indent}[bullet/] Endurance: {$endurance}
plant-analyzer-printout-l5 = {$indent}[bullet/] Lifespan: {$lifespan}
plant-analyzer-printout-l6 = {$indent}[bullet/] Product: [color=#a4885c]{$produce}[/color]
plant-analyzer-printout-l7 = [bullet/] Growth profile:
plant-analyzer-printout-l8 = {$indent}[bullet/] Water: [color=cyan]{$water}[/color]
plant-analyzer-printout-l9 = {$indent}[bullet/] Nutrition: [color=orange]{$nutrients}[/color]
plant-analyzer-printout-l10 = {$indent}[bullet/] Toxins: [color=yellowgreen]{$toxins}[/color]
plant-analyzer-printout-l11 = {$indent}[bullet/] Pests: [color=magenta]{$pests}[/color]
plant-analyzer-printout-l12 = {$indent}[bullet/] Weeds: [color=red]{$weeds}[/color]
plant-analyzer-printout-l13 = [bullet/] Environmental profile:
plant-analyzer-printout-l14 = {$indent}[bullet/] Composition: [bold]{$gasesIn}[/bold]
plant-analyzer-printout-l15 = {$indent}[bullet/] Pressure: [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color]
plant-analyzer-printout-l16 = {$indent}[bullet/] Temperature: [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color]
plant-analyzer-printout-l17 = {$indent}[bullet/] Light: [color=gray][bold]{$lightLevel} ± {$lightTolerance}[/bold][/color]
plant-analyzer-printout-l18 = [bullet/] Flowers: [color=lightgreen]{$n} {$potency}[/color]
plant-analyzer-printout-l19 = [bullet/] Seeds: {$seeds ->
    [no][color=red]No[/color]
    [yes][color=green]Yes[/color]
    *[other]{LOC("plant-analyzer-printout-missing")}
}
plant-analyzer-printout-l20 = [bullet/] Chemicals: [color=gray][bold]{$chemicals}[/bold][/color]
plant-analyzer-printout-l21 = [bullet/] Emissions: [bold]{$gasesOut}[/bold]
