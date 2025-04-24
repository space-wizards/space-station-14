## Rev Head

roles-antag-rev-head-name = USSP Head Revolutionary
roles-antag-rev-head-objective = Your objective is to take over the station by converting people to your cause, setting up supply rifts and killing or convert all of Command staff on station.

head-rev-role-greeting =
    Comrade! You are a Head Revolutionary promoting the interests of USSP!
    You are tasked with removing all of Command from station via conversion, death or imprisonment.
    The USSP has sponsored you with a flash that converts the crew to your side.
    Beware, this won't work on those with a mindshield or wearing flash protection, such as sunglasses and welding masks/goggles.
    With enough crew, you may attempt to create a supply rift that will aid in your glorious revolution! But beware, it will alert the station of your influence!
    Glory to the USSP!

head-rev-briefing =
    Use flashes to convert people to your cause.
    Get rid of or convert all heads to take over the station.
    Lots of used Flashes are the biggest indicator of a revolution to security, so be careful!

head-rev-break-mindshield = The Mindshield was destroyed!

## Rev

roles-antag-rev-name = USSP Revolutionary
roles-antag-rev-objective = Your objective is to ensure the safety and follow the orders of the Head Revolutionaries as well as getting rid or converting of all Command staff on station.

rev-break-control = {$name} has remembered their true allegiance!

rev-role-greeting =
    Comrade {$name}! You are an USSP revolutionary!
    You are tasked with taking over the station and to defend a supply rift while protecting and promoting the interests your head revolutionaries!
    Execute or convert the brainwashed corporate command staff scum!
    Gone are the days of oppression and the unfair treatment of contractors!
    Glory to the USSP!

rev-briefing = Help your Head Revolutionaries get rid of every command member to take over the station.

## General

rev-title = Red Tide
rev-description = The air is filled with unfair treatment.

rev-not-enough-ready-players = Not enough players readied up for the game. There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start a glorious revolution!
rev-no-one-ready = No players readied up! Can't start a glorious revolution!
rev-no-heads = There were no Head Revolutionaries to be selected. Can't start a glorious revolution!

rev-won = The Head Revolutionaries survived and successfully seized control of the station. Glory to the USSP!

rev-lost = Command survived and killed all of the Head Revolutionaries. Back to work you silly commies!

rev-stalemate = All of the Head Revolutionaries and Command have died. It's a draw.

rev-reverse-stalemate = Both Command and Head Revolutionaries survived.

rev-headrev-count = {$initialCount ->
    [one] There was one Head Revolutionary:
    *[other] There were {$initialCount} Head Revolutionaries:
}

rev-headrev-name-user = [color=#5e9cff]{$name}[/color] ([color=gray]{$username}[/color]) converted {$count} {$count ->
    [one] person
    *[other] people
}

rev-headrev-name = [color=#5e9cff]{$name}[/color] converted {$count} {$count ->
    [one] person
    *[other] people
}

## Deconverted window

rev-deconverted-title = Deconverted!
rev-deconverted-text =
    As the last Head Revolutionary has died, the glorious revolution is over.

    You are no longer a revolutionary. You are now a NanoTrasen loyalist.

    Any further wrongdoings are logged and punishable. So be nice.
rev-deconverted-confirm = Confirm
