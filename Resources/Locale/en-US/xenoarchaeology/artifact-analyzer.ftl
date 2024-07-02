analysis-console-menu-title = Broad-Spectrum Mark 3 Analysis Console
analysis-console-server-list-button = Server List
analysis-console-scan-button = Scan
analysis-console-scan-tooltip-info = Scan artifacts to learn information about their structure.
analysis-console-print-button = Print
analysis-console-print-tooltip-info = Print out the current information about the artifact.
analysis-console-extract-button = Extract
analysis-console-extract-button-info = Extract points from an artifact based on the newly explored nodes.
analysis-console-bias-up = Up
analysis-console-bias-down = Down
analysis-console-bias-button-info-up = Toggles the bias an artifact has in moving between its nodes. Up heads toward zero depth.
analysis-console-bias-button-info-down = Toggles the bias an artifact has in moving between its nodes. Down heads toward ever-higher depths.

analysis-console-info-no-scanner = No analyzer connected! Please connect one using a multitool.
analysis-console-info-no-artifact = No artifact present! Place one on the pad then scan for information.
analysis-console-info-ready = Systems operational. Ready to scan.

analysis-console-no-node = [font="Monospace" size=11]Select node to view[/font]
analysis-console-info-id = [font="Monospace" size=11]Node ID:[/font]
analysis-console-info-id-value = [font="Monospace" size=11][color=yellow]{$id}[/color][/font]
analysis-console-info-locked = [font="Monospace" size=11]Status:[/font]
analysis-console-info-locked-value = [font="Monospace" size=11][color={ $state ->
    [true] red]Locked
    *[false] lime]Unlocked
}[/color][/font]
analysis-console-info-active = [font="Monospace" size=11]Activity:[/font]
analysis-console-info-active-value = [font="Monospace" size=11][color={ $state ->
    [true] orange]Active
    *[false] gray]Inactive
}[/color][/font]
analysis-console-info-durability = [font="Monospace" size=11]Durability:[/font]
analysis-console-info-durability-value = [font="Monospace" size=11][color={$color}]{$current}/{$max}[/color][/font]
analysis-console-info-effect = [font="Monospace" size=11]Effect:[/font]
analysis-console-info-effect-value = [font="Monospace" size=11][color=gray]{ $state ->
    [true] WIP
    *[false] Unlock nodes to gain info
}[/color][/font]
analysis-console-info-trigger = [font="Monospace" size=11]Triggers:[/font]
analysis-console-info-triggered-value = [font="Monospace" size=11][color=gray]{$triggers}[/color][/font]
analysis-console-info-scanner = Scanning...
analysis-console-info-scanner-paused = Paused.
analysis-console-progress-text = {$seconds ->
    [one] T-{$seconds} second
    *[other] T-{$seconds} seconds
}
analysis-console-no-server-connected = Cannot extract. No server connected.
analysis-console-no-artifact-placed = No artifact on scanner.
analysis-console-no-points-to-extract = No points to extract.

analyzer-artifact-extract-popup = Energy shimmers on the artifact's surface!
