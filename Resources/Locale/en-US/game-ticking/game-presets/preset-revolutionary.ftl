## Rev Head

roles-antag-rev-head-name = USSP SKB agent
roles-antag-rev-head-objective = Your objective is to take over the station by bringing back people to your cause, setting up supply rifts, killing, converting or imprisoning all of Command staff on station.

head-rev-role-greeting =
    Comrade {$name}! You are a recruting agent promoting the interests of USSP!
    You are tasked with taking over the station by removing all of Command via conversion, death or imprisonment.
    The SKB has sponsored you with a flash that brings contractors to your side.
    Beware, this won't work on those brainwashed with a 'mindshield' or wearing flash protection, such as sunglasses and welding masks or goggles.
    With enough crew, you may attempt to create a supply rift that will aid in your glorious revolution! But beware, it will alert the station of your influence!
    Glory to the USSP!

head-rev-briefing =
    Use flashes bring people to your cause.
    Get rid of or convert all heads to take over the station.
    Lots of used flashes are the biggest indicator of a revolution to security, so be careful!

head-rev-break-mindshield = The MindShield™ was destroyed!

## Rev

roles-antag-rev-name = USSP Revolutionary
roles-antag-rev-objective = Your objective is to ensure the safety of the SKB agents, to follow their orders, and to get rid or convert of all Command staff on station.

rev-break-control = {$name} has remembered their true allegiance!

rev-role-greeting =
    Comrade {$name}! You are an USSP revolutionary!
    You are tasked with taking over the station and to promote the interests of the soviet agent who recruited you!
    Execute, imprison or convert the brainwashed corporate command staff scum!
    Gone are the days of oppression and the unfair treatment of contractors!
    Glory to the USSP!

rev-briefing = Help your soviet agent get rid of every command member to take over the station.

## General

rev-title = Red Tide
rev-description = The air is filled with unfair treatment.

rev-not-enough-ready-players = Not enough players readied up for the game. There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start a glorious revolution!
rev-no-one-ready = No players readied up! Can't start a glorious revolution!
rev-no-heads = There were no revolutionary agents to be selected. Can't start a glorious revolution!

rev-won = [color=red]The SKB agents survived and seized control of the station![/color]

rev-lost = Command survived and killed all the SKB agents.

rev-stalemate = All of the SKB agents and command have died. It's a draw.

rev-reverse-stalemate = Both Command and SKB agents survived.

central-command-revolution-announcement = Based on our scans from our long-range sensors, we believe the station has fallen under the control of hostile revolutionary forces. All heads of staff have been confirmed deceased or missing. All remaining crew members are to stand by for further instructions.

soviet-commissariat-revolution-announcement = Long range communications array online. Motherland salutes you comrades, but the battle is not yet over. Your corporation will check if they can reclaim your station one last time, but do not worry! The SSF will arrive shorty. Glory to the USSP!

central-command-sender = Central Command

soviet-commissariat-sender = Soviet People's Commissariat

rev-headrev-count = {$initialCount ->
    [one] There was one agent of [color=Yellow]USSP[/color]:
    *[other] There were {$initialCount} agents of USSP:
}

rev-headrev-name-user = [color=#5e9cff]{$name}[/color] ([color=gray]{$username}[/color]) recruited {$count} {$count ->
    [one] contractor
    *[other] contractors
}

rev-headrev-name = [color=#5e9cff]{$name}[/color] recruited {$count} {$count ->
    [one] contractor
    *[other] contractors
}

## Deconverted window

rev-deconverted-title = Reconverted!
rev-deconverted-text =
    As the last soviet agent has died, the glorious revolution is now over.

    You are no longer a revolutionary. You now return back as NanoTrasen contractor.

    Any further wrongdoings are logged and punishable. So be nice.
rev-deconverted-confirm = Understood
