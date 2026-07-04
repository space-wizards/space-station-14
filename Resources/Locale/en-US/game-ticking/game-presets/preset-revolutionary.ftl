## Rev Head

roles-antag-rev-head-name = Head Revolutionary
roles-antag-rev-head-objective = Your objective is to take over the station by converting people to your cause and eliminating all members of Command.

head-rev-role-greeting =
    You are a head revolutionary. You are tasked with removing all of Command from power through death, restraint, or conversion.
    The Syndicate has sponsored you with a flash that converts others to your cause. Beware, this won't work on those with eye protection or mindshield implants. Remember that Command and Security are implanted with mindshields as part of the hiring process.
    Viva la revolución!

head-rev-briefing =
    Use flashes to convert people to your cause.
    Kill, restrain, or convert all members of Command to take over the station.

head-rev-break-mindshield = The mindshield implant was destroyed!

## Rev

roles-antag-rev-name = Revolutionary
roles-antag-rev-objective = Your objective is to ensure the safety and follow the orders of the head revolutionaries, and to help them take over the station by eliminating all members of Command.

rev-break-control = {$name} has remembered their true allegiance!

rev-role-greeting =
    You are a revolutionary. You are tasked with protecting the head revolutionaries and helping them take over the station.
    The revolution must work together to kill, restrain, or convert all members of Command.
    Viva la revolución!

rev-briefing = Help the head revolutionaries kill, restrain, or convert all members of Command to take over the station.

## General

rev-title = Revolutionaries
rev-description = Revolutionaries hidden among the crew are seeking to convert others to their cause and overthrow Command.

rev-not-enough-ready-players = Not enough players readied up for the game. There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start Revolutionaries.
rev-no-one-ready = No players readied up! Can't start Revolutionaries.
rev-no-heads = There were no Head Revolutionaries to be selected. Can't start Revolutionaries.

rev-won = The head revolutionaries survived and successfully seized control of the station.

rev-lost = All head revolutionaries have died, and Command survived.

rev-stalemate = Both Command and the head revolutionaries have all died. It's a draw.

rev-reverse-stalemate = Both Command and the head revolutionaries survived.

rev-headrev-count = {$initialCount ->
    [one] There was one head revolutionary:
    *[other] There were {$initialCount} head revolutionaries:
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
    As the last head revolutionary has died, the revolution is over.

    You are no longer a revolutionary, so be nice.
rev-deconverted-confirm = Confirm
