## Rev Head

roles-antag-rev-head-name = Head Revolutionary
roles-antag-rev-head-objective = Your objective is to take over the station by converting people to your cause and killing all Command staff on station.

head-rev-role-greeting =
    You are a Head Revolutionary.
    You are tasked with removing all of Command from station via death or exilement.
    The Syndicate has sponsored you with a flash that converts the crew to your side.
    Beware, this won't work on Security, Command, or those wearing sunglasses.
    Viva la revolución!

head-rev-briefing =
    Use flashes to convert people to your cause.
    Kill all heads to take over the station.

head-rev-break-mindshield = The Mindshield was destroyed!

## Rev

roles-antag-rev-name = Revolutionary
roles-antag-rev-objective = Your objective is to ensure the safety and follow the orders of the Head Revolutionaries as well as killing all Command staff on station.

rev-break-control = {$name} has remembered their true allegiance!

rev-role-greeting =
    You are a Revolutionary.
    You are tasked with taking over the station and protecting the Head Revolutionaries.
    Eliminate all of the Command staff.
    Viva la revolución!

rev-briefing = Help your head revolutionaries kill every head to take over the station.

## General

rev-title = Revolutionaries
rev-description = Revolutionaries are among us.

rev-not-enough-ready-players = Not enough players readied up for the game. There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start a Revolution.
rev-no-one-ready = No players readied up! Can't start a Revolution.
rev-no-heads = There were no Head Revolutionaries to be selected. Can't start a Revolution.

rev-all-heads-dead = All the heads are dead, now finish up the rest of the crew!

rev-won = The Head Revs survived and killed all of Command.

rev-lost = Command survived and killed all of the Head Revs.

rev-stalemate = All of the Head Revs and Command died. It's a draw.

rev-reverse-stalemate = Both Command and Head Revs survived.

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
