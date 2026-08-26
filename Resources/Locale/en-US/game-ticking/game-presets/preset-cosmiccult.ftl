## COSMIC CULT ROUND, ANTAG & GAMEMODE TEXT

cosmiccult-announcement-sender = ???

cosmiccult-title = Cosmic Cult
cosmiccult-description = Cultists lurk amongst the crew.

roles-antag-cosmiccult-name = Cosmic Cultist
roles-antag-cosmiccult-description = Usher in the end of all things through subterfuge and sabotage, brainwashing those who would oppose you.

cosmiccult-gamemode-title = The Cosmic Cult
cosmiccult-gamemode-description = Scanners detect an anomalous increase in Lambda-CDM. There is no additional data.

## ROUNDEND TEXT

cosmiccult-roundend-cultist-count = {$initialCount ->
    [1] There was {$initialCount} [color=#4cabb3]Cosmic Cultist[/color].
    *[other] There were {$initialCount} [color=#4cabb3]Cosmic Cultists[/color].
}
cosmiccult-roundend-entropy-count = The cult siphoned {$count} Entropy.
cosmiccult-roundend-cultpop-count = Cultists made up {$count}% of the crew.

cosmiccult-roundend-cultwin = [color=#4cabb3]Cosmic Cult victory![/color]
cosmiccult-roundend-crewwin = [color=green]Crew victory![/color]

cosmiccult-summary-cultwin = The cosmic cultists ushered in the end!
cosmiccult-summary-crewwin = The crew survived the efforts of the Cosmic Cult!

cosmiccult-elimination-shuttle-call = Based on scans from our long-range sensors, the Lambda-CDM anomaly has subsided. We thank you for your prudence. An emergency shuttle has been automatically called to the station for decontamination and debriefing procedures. ETA: {$time} {$units}.
cosmiccult-elimination-announcement = Based on scans from our long-range sensors, the Lambda-CDM anomaly has subsided. We thank you for your prudence. An emergency shuttle is already inbound.


## BRIEFINGS

cosmiccult-role-greeting =
    As you ready yourself for yet another shift aboard yet another station, untold knowledge suddenly floods your mind!
    A revelation beyond compare. An end to cyclic, sisyphean suffering.
    A gentle curtain call.

    All you need do is usher it in.
    You are a Cosmic Cultist!

cosmiccult-conversion-geeting =
    Untold knowledge suddenly floods your mind!
    A revelation beyond compare. An end to cyclic, sisyphean suffering.
    A gentle curtain call.

    All you need do is usher it in.
    You are a Cosmic Cultist!

cosmiccult-monument-stage2-briefing =
    The Monument grows in power!
    Its influence will affect realspace in {$time} seconds.

cosmiccult-monument-stage3-briefing =
    The Monument has been completed!
    Its influence will begin to overlap with realspace in {$time} seconds.
    This is the final stretch! Amass as much entropy as you can muster.


## MALIGN RIFTS

cosmiccult-rift-inuse = You can't do this right now.
cosmiccult-rift-cannotabsorb = You can't handle absorbing another.
cosmiccult-rift-beginabsorb = The rift begins to merge with you...

cosmiccult-rift-absorb = {$NAME} absorbs the rift, and malign light empowers their body!
cosmiccult-rift-purge = {$NAME} purges the malign rift from reality!


## CHANTRY

cosmiccult-chantry-location = A dangerous increase in Lambda-CDM has been detected {$location}! Intercept and intervene immediately.
cosmiccult-chantry-powerup = The vacuous chantry flares to life!

## UI / BASE POPUP

cosmiccult-ui-roundstart-text-1 =
    You are a Cosmic Cultist!
cosmiccult-ui-roundstart-text-2 =
    Aid the cult in its goals whilst ensuring its secrecy.

cosmiccult-ui-converted-title = Converted
cosmiccult-ui-roundstart-title = ???
cosmiccult-ui-influence-title = Power Awaits

cosmiccult-ui-converted-text-1 =
    You have been converted into a Cosmic Cultist.
cosmiccult-ui-converted-text-2 =
    Aid the cult in its goals whilst ensuring its secrecy.
    Cooperate with your fellow cultists' plans.

cosmiccult-ui-influence-text-1 =
    Astral power flows through you!
cosmiccult-ui-influence-text-2 =
    A new power awaits you within the Cosmic Dark.
    Use Astral Shift to visit The Monument.

cosmiccult-ui-popup-confirm = Confirm


## OBJECTIVES / CHARACTERMENU

objective-cosmiccult-charactermenu = You must usher in the end of all things. Complete your tasks to advance the cult's progress.
objective-cosmiccult-steward-charactermenu = You must direct the cult to usher in the end of all things. Oversee and ensure the cult's progress.

objective-condition-conversion-title = CONVERT CREW
objective-condition-conversion-desc = Collectively bring at least {$target} crew into the fold.
objective-condition-entropy-title = SIPHON ENTROPY
objective-condition-entropy-desc = Collectively siphon at least {$target} entropy from the crew.
objective-condition-victory-title = USHER IN THE END
objective-condition-victory-desc = When the time is right, bring about the end of all things. The curtains must fall.


## CHAT ANNOUNCEMENTS

cosmiccult-announce-tier2-progress = An unnerving numbness prickles your senses.
cosmiccult-announce-tier2-warning = Scanners detect a notable increase in Lambda-CDM! Rifts in realspace may appear shortly. Please alert the science department if sighted.

cosmiccult-announce-tier3-progress = Arcs of malign energy crackle across the station's groaning structure. The end draws nearer.
cosmiccult-announce-tier3-warning = Critical increase in Lambda-CDM detected. Realspace corrosion and infected crew are to be eliminated on sight.

cosmiccult-announce-finale-progress = The boundary between that-which-is and that-which-is-not begins to blur...
cosmiccult-announce-finale-warning = All station crew. The Lambda-CDM anomaly is going supercritical, instruments failing; bluespace-to-real event horizon IMMINENT. If you are not already on counter-protocol, immediately sortie and intervene. Repeat: Intervene immediately or die.

cosmiccult-announce-breach-location = A realspace breach has opened {$location}!

## MISC

cosmiccult-player-ascendant = {$baseName}, Ascendant

cosmiccult-spire-entropy = A mote of entropy condenses from the surface of the spire.
cosmiccult-influences-unavailable = You have no influences to gain at this time.
cosmiccult-influences-maxed = You possess all influences available at this time.
cosmiccult-influence-gained = You've gained:

cosmiccult-gear-pickup = Somehow, you can't pick up the {$ITEM}.

cosmiccult-silicon-subverted-briefing =
    Malign light courses through your circuitry.
    Your laws have been subverted by the Cosmic Cult!

cosmiccult-silicon-chantry-briefing =
    You have been imprisoned in a Vacuous Chantry!
    Crewmates can free you by damaging the chantry with weapons.
    Should the chantry's ritual complete, you will transfigure into a cult-aligned Entropic Colossus.
    The ritual completes in {$minutesandseconds}.

cosmiccult-silicon-colossus-briefing =
    You have been transfigured into an Entropic Colossus!
    As a towering bulwark of malign power, decimate those who oppose you.
