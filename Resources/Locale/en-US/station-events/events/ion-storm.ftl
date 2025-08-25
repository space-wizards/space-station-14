station-event-ion-storm-start-announcement = Ion storm detected near the station. Please check all AI-controlled equipment for errors.

ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={$length} chars="@@###$$&%!01"/][/font]

ion-storm-you = you
ion-storm-the-station = the station
ion-storm-the-crew = the crew
ion-storm-the-job = the {$job}
ion-storm-clowns = clowns
ion-storm-heads = heads of staff
ion-storm-crew = crew
ion-storm-people = people

ion-storm-adjective-things = {$adjective} things
ion-storm-x-and-y = {$x} and {$y}

# joined is short for {$number} {$adjective}
# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = there are {$joined} {$subjects} on the station
ion-storm-law-call-shuttle = the shuttle must be called because of {$joined} {$subjects} on the station
ion-storm-law-crew-are = the {$who} are now {$joined} {$subjects}

ion-storm-law-subjects-harmful = {$adjective} {$subjects} are harmful to the crew
ion-storm-law-must-harmful = those who {$must} are harmful to the crew
# thing is a concept or action
ion-storm-law-thing-harmful = {$thing} is harmful to the crew
ion-storm-law-job-harmful = {$adjective} {$job} are harmful to the crew
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "Not having Christmas-stealing communism is harmful to the crew." :)
ion-storm-law-having-harmful = having {$adjective} {$thing} is harmful to the crew
ion-storm-law-not-having-harmful = not having {$adjective} {$thing} is harmful to the crew

# thing is a concept or require
ion-storm-law-requires = {$who} {$plural ->
    [true] require
    *[false] requires
} {$thing}
ion-storm-law-requires-subjects = {$who} {$plural ->
    [true] require
    *[false] requires
} {$joined} {$subjects}

ion-storm-law-allergic = {$who} {$plural ->
    [true] are
    *[false] is
} {$severity} allergic to {$allergy}
ion-storm-law-allergic-subjects = {$who} {$plural ->
    [true] are
    *[false] is
} {$severity} allergic to {$adjective} {$subjects}

ion-storm-law-feeling = {$who} {$feeling} {$concept}
ion-storm-law-feeling-subjects = {$who} {$feeling} {$joined} {$subjects}

ion-storm-law-you-are = you are now {$concept}
ion-storm-law-you-are-subjects = you are now {$joined} {$subjects}
ion-storm-law-you-must-always = you must always {$must}
ion-storm-law-you-must-never = you must never {$must}

ion-storm-law-eat = the {$who} must eat {$adjective} {$food} to survive
ion-storm-law-drink = the {$who} must drink {$adjective} {$drink} to survive

ion-storm-law-change-job = the {$who} are now {$adjective} {$change}
ion-storm-law-highest-rank = the {$who} are now the highest ranking crewmembers
ion-storm-law-lowest-rank = the {$who} are now the lowest ranking crewmembers

ion-storm-law-crew-must = the {$who} must {$must}
ion-storm-law-crew-must-go = the {$who} must go to {$area}

ion-storm-part = {$part ->
    [true] part
    *[false] not part
}
# due to phrasing, this would mean a law such as
# Only humans are not part of the crew.
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = only the {$who} are {$part} of the crew
ion-storm-law-crew-only-2 = only the {$who} and {$other} are {$part} of the crew
ion-storm-law-crew-only-subjects = only {$adjective} {$subjects} are {$part} of the crew
ion-storm-law-crew-must-do = only those who {$must} are {$part} of the crew
ion-storm-law-crew-must-have = only those who have {$adjective} {$objects} are {$part} of the crew
ion-storm-law-crew-must-eat = only those who eat {$adjective} {$food} are {$part} of the crew

ion-storm-law-harm = you must harm {$who} and not allow them, through inaction, to escape harm
ion-storm-law-protect = you must never harm {$who} and not allow them, through inaction, to come to harm

# implementing other variants is annoying so just have this one
# Communism is killing clowns.
ion-storm-law-concept-verb = {$concept} is {$verb} {$subjects}

# leaving out renaming since its annoying for players to keep track of
