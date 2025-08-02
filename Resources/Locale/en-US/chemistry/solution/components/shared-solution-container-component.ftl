shared-solution-container-component-on-examine-empty-container = [color=gray]Contains no chemicals.[/color]
shared-solution-container-component-on-examine-main-text = It contains {INDEFINITE($desc)} [color={$color}]{$desc}[/color] { $chemCount ->
    [1] chemical.
    *[other] mixture of chemicals.
    }

# Legacy names
drink-component-on-examine-is-full = The solution is [color=white]full[/color].
drink-component-on-examine-is-mostly-full = The solution is [color=white]mostly full[/color].
drink-component-on-examine-is-half-full = The solution is [color=white]halfway full[/color].
drink-component-on-examine-is-half-empty = The solution is [color=white]halfway empty[/color].
drink-component-on-examine-is-mostly-empty = The solution is [color=white]mostly empty[/color].
drink-component-on-examine-exact-volume = The solution contains [color=white]{$amount}u[/color].

examinable-solution-has-recognizable-chemicals = You can recognize {$recognizedString} in the solution.
examinable-solution-recognized-first = [color={$color}]{$chemical}[/color]
examinable-solution-recognized-next = , [color={$color}]{$chemical}[/color]
# Special handling to include a space at the start of the line
examinable-solution-recognized-last = {" "}and [color={$color}]{$chemical}[/color]
