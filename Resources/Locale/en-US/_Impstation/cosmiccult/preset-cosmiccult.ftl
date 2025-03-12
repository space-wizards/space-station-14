## COSMIC CULT ROUND, ANTAG & GAMEMODE TEXT

cosmiccult-announcement-sender = ???

cosmiccult-title = Cosmic Cult
cosmiccult-description = Cultists lurk amongst the crew.

roles-antag-cosmiccult-name = Cosmic Cultist
roles-antag-cosmiccult-description = Usher in the end of all things through subterfuge and sabotage, brainwashing those who would oppose you.

roles-antag-cosmiccultlead-name = Cosmic Cult Leader
roles-antag-cosmiccultlead-description = Organize the cult into a force to be reckoned with, so that you may usher in the end of all things.

cosmiccult-gamemode-title = The Cosmic Cult
cosmiccult-gamemode-description =
    Scanners detect an anomalous increase in Λ-CDM. There is no additional data.


cosmiccult-finale-autocall-briefing = The Monument activates {$minutesandseconds}! Gather yourselves, and prepare for the end.
cosmiccult-finale-ready = A terrifying light surges forth from The Monument!
cosmiccult-finale-count = There are {$COUNT} cultists empowering the monument..
cosmiccult-finale-cultist-count = {$COUNT ->
    [0] The summoning is stagnant..
    [1] The summoning begins to quicken.
    [2] The summoning begins to quicken..
    [3] The summoning is slowly quickening..
    [4] The summoning quickens.
    [5] The summoning quickens faster.
    [6] The summoning is hastened!
    *[other] The summoning is hastened by {$COUNT} cultists present!
}

cosmiccult-finale-degen = You feel yourself unravelling!
cosmiccult-finale-location = Scanners are detecting an enormous Λ-CDM spike {$location}!
cosmiccult-finale-cancel-begin = You begin to disrupt The Monument's energies..
cosmiccult-finale-beckon-begin = You begin to Beckon The Unknown..
cosmiccult-finale-beckon-success = You beckon for the final curtain call.

cosmiccult-monument-powerdown = The Monument falls silent..

## ROUNDEND TEXT

cosmiccult-roundend-cultist-count = {$initialCount ->
    [1] There was {$initialCount} [color=#4cabb3]Cosmic Cultist[/color].
    *[other] There were {$initialCount} [color=#4cabb3]Cosmic Cultists[/color].
}
cosmiccult-roundend-entropy-count = The cult siphoned {$count} Entropy.
cosmiccult-roundend-cultpop-count = Cultists made up {$count}% of the crew.
cosmiccult-roundend-monument-stage = {$stage ->
    [1] The Monument was abandoned.
    [2] The Monument made some progress.
    [3] The Monument was completed!
    *[other] [color=red]Something went REALLY wrong.[/color]
}

cosmiccult-roundend-cultcomplete = [color=#4cabb3]Cosmic Cult complete victory![/color]
cosmiccult-roundend-cultmajor = [color=#4cabb3]Cosmic Cult major victory![/color]
cosmiccult-roundend-cultminor = [color=#4cabb3]Cosmic Cult minor victory![/color]
cosmiccult-roundend-neutral = [color=yellow]Neutral ending![/color]
cosmiccult-roundend-crewminor = [color=green]Crew minor victory![/color]
cosmiccult-roundend-crewmajor = [color=green]Crew major victory![/color]
cosmiccult-roundend-crewcomplete = [color=green]Crew complete victory![/color]

cosmiccult-summary-cultcomplete = The cosmic cultists ushered in the end!
cosmiccult-summary-cultmajor = The cosmic cultists' victory will be inevitable.
cosmiccult-summary-cultminor = The Monument was completed, but not fully empowered.
cosmiccult-summary-neutral = The cult will live to see another day.
cosmiccult-summary-crewminor = The cult has been left leaderless.
cosmiccult-summary-crewmajor = All cosmic cultists were eliminated.
cosmiccult-summary-crewcomplete = Every single cosmic cultist was deconverted!

cosmiccult-elimination-shuttle-call = Based on scans from our long-range sensors, the Λ-CDM anomaly has subsided. We will call emergency shuttle that will arrive shortly. ETA: {$time} {$units}. You can recall the shuttle to extend the shift.
cosmiccult-elimination-announcement = Based on scans from our long-range sensors, the Λ-CDM anomaly has subsided. Shuttle is already called.


## BRIEFINGS

cosmiccult-role-roundstart-fluff =
    As you ready yourself for yet another shift aboard yet another NanoTrasen station, untold knowledge suddenly floods your mind!
    A revelation beyond compare. An end to cyclic, sisyphean suffering.
    A gentle curtain call.

    All you need do is usher it in.

cosmiccult-role-short-briefing =
    You are a Cosmic Cultist!
    Your objectives are listed in the character menu.
    Read more about your role in the guidebook entry.

cosmiccult-role-conversion-fluff =
    As the invocation completes, untold knowledge suddenly floods your mind!
    A revelation beyond compare. An end to cyclic, sisyphean suffering.
    A gentle curtain call.

    All you need do is usher it in.

cosmiccult-role-deconverted-fluff =
    A great emptiness washes across your mind. A comforting, yet unfamiliar emptiness...
    All the thoughts and memories of your time in the cult begin to fade and blur.

cosmiccult-role-deconverted-briefing =
    Deconverted!
    You are no longer a Cosmic Cultist.

cosmiccult-monument-stage2-briefing =
    The Monument grows in power!
    Its influence will affect realspace in {$time} seconds.

cosmiccult-monument-stage3-briefing =
    The Monument has been completed!
    Its influence will begin to overlap with realspace in {$time} seconds.
    This is the final stretch! Amass as much entropy as you can muster.

## MALIGN RIFTS

cosmiccult-rift-inuse = You can't do this right now.
cosmiccult-rift-invaliduser = You lack the tools to deal with this.
cosmiccult-rift-chaplainoops = You should probably use a bible or gildgrail.
cosmiccult-rift-alreadyempowered = You don't need to absorb another one.
cosmiccult-rift-beginabsorb = You begin absorbing the malign rift..
cosmiccult-rift-beginpurge = You begin purging the malign rift..

cosmiccult-rift-absorb = You absorb the malign rift, empowering yourself.
cosmiccult-rift-purge = You purge the malign rift.



## UI / BASE POPUP

cosmiccult-ui-deconverted-title = Deconverted
cosmiccult-ui-converted-title = Converted
cosmiccult-ui-roundstart-title = The Unknown

cosmiccult-ui-converted-text-1 =
    You have been converted into a Cosmic Cultist.
cosmiccult-ui-converted-text-2 =
    Aid the cult in its goals whilst ensuring its secrecy.
    Cooperate with your fellow cultists' plans.

cosmiccult-ui-roundstart-text-1 =
    You are a Cosmic Cultist!
cosmiccult-ui-roundstart-text-2 =
    Aid the cult in its goals whilst ensuring its secrecy.
    Listen to your cult leader's directions.

cosmiccult-ui-deconverted-text-1 =
    You are no longer a Cosmic Cultist.
cosmiccult-ui-deconverted-text-2 =
    You have lost all memories pertaining to the Cosmic Cult.
    If you are converted back, these memories will return.

cosmiccult-ui-popup-confirm = Confirm



## OBJECTIVES / CHARACTERMENU

objective-issuer-cosmiccult = [bold][color=#cae8e8]The Unknown[/color][/bold]

objective-cosmiccult-charactermenu = You must usher in the end of all things. Complete your tasks to advance the cult's progress.
objective-cosmiccultlead-charactermenu = You must lead the cult to usher in the end of all things. Oversee and ensure the cult's progress.

objective-condition-entropy-title = SIPHON ENTROPY
objective-condition-entropy-desc = Collectively siphon at least {$count} entropy from the crew.
objective-condition-culttier-title = EMPOWER THE MONUMENT
objective-condition-culttier-desc = Ensure that The Monument is brought to full power.
objective-condition-victory-title = USHER IN THE END
objective-condition-victory-desc = Beckon The Unknown, and herald the final curtain call.

## CHAT ANNOUNCEMENTS

cosmiccult-radio-tier1-progress = The monument is beckoned unto realspace..
cosmiccult-announce-tier2-progress = An unnerving numbness prickles your senses.
cosmiccult-announce-tier2-warning = Scanners detect a notable increase in Λ-CDM! Rifts in realspace may appear shortly. Please alert your station's chaplain if sighted.

cosmiccult-announce-tier3-progress = Arcs of bluespace energy crackle across the station's groaning structure. The end draws near.
cosmiccult-announce-tier3-warning = Critical increase in Λ-CDM detected. Infected personnel are to be neutralized.

cosmiccult-announce-victory-summon = A FRACTION OF COSMIC POWER IS CALLED FORTH.


## MISC

cosmiccult-spire-entropy = A mote of entropy condenses from the surface of the spire.
cosmiccult-entropy-inserted = You infuse {$count} entropy into The Monument.
cosmiccult-entropy-unavailable = You can't do that right now.
cosmiccult-astral-ascendant = {$NAME}, Ascendant
cosmiccult-gear-pickup-rejection = The {$ITEM} resists {CAPITALIZE(THE($TARGET))}'s touch!
