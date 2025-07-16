## Rev Head

roles-antag-rev-head-name = Head Revolutionary
roles-antag-rev-head-objective = Your objective is to take over the station by converting people to your cause and killing all Command staff on station.

head-rev-role-greeting =
    You are a Head Revolutionary.
    You are tasked with removing all of Command from station via conversion, death or imprisonment.
    The Syndicate has sponsored you with a flash that converts the crew to your side.
    Beware, this won't work on those with a mindshield or wearing eye protection.
    Viva la revolución!

head-rev-briefing =
    Use flashes to convert people to your cause.
    Get rid of or convert all heads to take over the station.

head-rev-break-mindshield = The Mindshield was destroyed!

## Rev

roles-antag-rev-name = Revolutionary
roles-antag-rev-objective = Your objective is to ensure the safety and follow the orders of the Head Revolutionaries as well as getting rid or converting of all Command staff on station.

rev-break-control = {$name} has remembered their true allegiance!

rev-role-greeting =
    You are a Revolutionary.
    You are tasked with taking over the station and protecting the Head Revolutionaries.
    Get rid of all of or convert the Command staff.
    Viva la revolución!

rev-briefing = Help your head revolutionaries get rid of every head to take over the station.

## General

rev-title = Revolutionaries
rev-description = Revolutionaries are among us.

rev-not-enough-ready-players = Not enough players readied up for the game. There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start a Revolution.
rev-no-one-ready = No players readied up! Can't start a Revolution.
rev-no-heads = There were no Head Revolutionaries to be selected. Can't start a Revolution.

rev-won = The Head Revs survived and successfully seized control of the station.

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

## Deconverted window

rev-deconverted-title = Deconverted!
rev-deconverted-text =
    As the last headrev has died, the revolution is over.

    You are no longer a revolutionary, so be nice.
rev-deconverted-confirm = Confirm

# New Rev Endscreen
rev-major = [color=#5e9cff]Revolution major victory![/color]
rev-minor = [color=#5e9cff]Revolution minor victory![/color]
rev-draw = [color=yellow]Neutral outcome![/color]
rev-crew-minor = [color=green]Crew minor victory![/color]
rev-crew-major = [color=green]Crew major victory![/color]
all-revs-failed = [color=crimson]All revolutionaries were killed or deconverted![/color]

rev-crew-percentage = [color={$color}]{$%}% of the crew were converted to the revolution.[/color]
rev-command-percentage = [color={$color}]{$%}% of Command were converted to the revolution.[/color]
rev-loyal-command = [color={$color}]{$count} loyal Command members escaped to CentComm alive and unrestrained.[/color]
headrev-escapes = [color={$color}]{$count} Head Revolutionaries escaped to CentComm alive and unrestrained.[/color]
