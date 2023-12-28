anomaly-component-contact-damage = The anomaly sears off your skin!

anomaly-vessel-component-anomaly-assigned = Anomaly assigned to vessel.
anomaly-vessel-component-not-assigned = This vessel is not assigned to any anomaly. Try using a scanner on it.
anomaly-vessel-component-assigned = This vessel is currently assigned to an anomaly.
anomaly-vessel-component-upgrade-output = point output

anomaly-particles-delta = Delta particles
anomaly-particles-epsilon = Epsilon particles
anomaly-particles-zeta = Zeta particles
anomaly-particles-omega = Omega particles

anomaly-scanner-component-scan-complete = Scan complete!

anomaly-scanner-ui-title = anomaly scanner
anomaly-scanner-no-anomaly = No anomaly currently scanned.
anomaly-scanner-severity-percentage = Current severity: [color=gray]{$percent}[/color]
anomaly-scanner-stability-low = Current anomaly state: [color=gold]Decaying[/color]
anomaly-scanner-stability-medium = Current anomaly state: [color=forestgreen]Stable[/color]
anomaly-scanner-stability-high = Current anomaly state: [color=crimson]Growing[/color]
anomaly-scanner-point-output = Point output: [color=gray]{$point}[/color]
anomaly-scanner-particle-readout = Particle Reaction Analysis:
anomaly-scanner-particle-danger = - [color=crimson]Danger type:[/color] {$type}
anomaly-scanner-particle-unstable = - [color=plum]Unstable type:[/color] {$type}
anomaly-scanner-particle-containment = - [color=goldenrod]Containment type:[/color] {$type}
anomaly-scanner-pulse-timer = Time until next pulse: [color=gray]{$time}[/color]

anomaly-gorilla-core-slot-name = Anomaly core
anomaly-gorilla-charge-none = It has no [bold]anomaly core[/bold] inside of it.
anomaly-gorilla-charge-limit = It has [color={$count ->
    [3]green
    [2]yellow
    [1]orange
    [0]red
    *[other]purple
}]{$count} {$count ->
    [one]charge
    *[other]charges
}[/color] remaining.
anomaly-gorilla-charge-infinite = It has [color=gold]infinite charges[/color]. [italic]For now...[/italic]

anomaly-sync-connected = Anomaly successfully attached
anomaly-sync-disconnected = The connection to the anomaly has been lost!
anomaly-sync-no-anomaly = No anomaly in range.
anomaly-sync-examine-connected = It is [color=darkgreen]attached[/color] to an anomaly.
anomaly-sync-examine-not-connected = It is [color=darkred]not attached[/color] to an anomaly.
anomaly-sync-connect-verb-text = Attach anomaly
anomaly-sync-connect-verb-message = Attach a nearby anomaly to {THE($machine)}.

anomaly-generator-ui-title = Anomaly Generator
anomaly-generator-fuel-display = Fuel:
anomaly-generator-cooldown = Cooldown: [color=gray]{$time}[/color]
anomaly-generator-no-cooldown = Cooldown: [color=gray]Complete[/color]
anomaly-generator-yes-fire = Status: [color=forestgreen]Ready[/color]
anomaly-generator-no-fire = Status: [color=crimson]Not ready[/color]
anomaly-generator-generate = Generate Anomaly
anomaly-generator-charges = {$charges ->
    [one] {$charges} charge
    *[other] {$charges} charges
}
anomaly-generator-announcement = An anomaly has been generated!

anomaly-command-pulse = Pulses a target anomaly
anomaly-command-supercritical = Makes a target anomaly go supercritical

# Flavor text on the footer
anomaly-generator-flavor-left = Anomaly may spawn inside the operator.
anomaly-generator-flavor-right = v1.1
