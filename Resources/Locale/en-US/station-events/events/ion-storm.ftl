station-event-ion-storm-start-announcement = Ion storm detected near the station. Please check all AI-controlled equipment for errors.

ion-storm-you = YOU
ion-storm-the-station = THE STATION
ion-storm-the-crew = THE CREW
ion-storm-the-job = THE {$job}
ion-storm-clowns = CLOWNS
ion-storm-heads = HEADS OF STAFF
ion-storm-crew = CREW

ion-storm-adjective-things = {$adjective} THINGS
ion-storm-x-and-y = {$x} AND {$y}

# joined is short for {$number} {$adjective}
# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = THERE ARE {$joined} {$subjects} ON THE STATION
ion-storm-law-no-shuttle = THE SHUTTLE CANNOT BE CALLED BECAUSE OF {$joined} {$subjects} ON THE STATION
ion-storm-law-crew-are = THE {$who} ARE NOW {$joined} {$subjects}

ion-storm-law-subjects-harmful = {$adjective} {$subjects} ARE HARMFUL TO THE CREW
ion-storm-law-must-harmful = THOSE WHO {$must} ARE HARMFUL TO THE CREW
# thing is a concept or action
ion-storm-law-thing-harmful = {$thing} IS HARMFUL TO THE CREW
ion-storm-law-job-harmful = {$adjective} {$job} ARE HARMFUL TO THE CREW
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
ion-storm-law-having-harmful = HAVING {$adjective} {$thing} IS HARMFUL TO THE CREW
ion-storm-law-not-having-harmful = NOT HAVING {$adjective} {$thing} IS HARMFUL TO THE CREW

# thing is a concept or require
ion-storm-law-requires = {$who} {$plural ->
    [true] REQUIRES
    *[false] REQUIRE
} {$thing}
ion-storm-law-requires-subjects = {$who} {$plural ->
    [true] REQUIRES
    *[false] REQUIRE
} {$joined} {$subjects}

ion-storm-law-allergic = {$who} {$plural ->
    [true] IS
    *[false] ARE
} {$severity} ALLERGIC TO {$allergy}
ion-storm-law-allergic-subjects = {$who} {$plural ->
    [true] IS
    *[false] ARE
} {$severity} ALLERGIC TO {$adjective} {$subjects}

ion-storm-law-feeling = {$who} {$feeling} {$concept}
ion-storm-law-feeling-subjects = {$who} {$feeling} {$joined} {$subjects}

ion-storm-law-you-are = YOU ARE NOW {$concept}
ion-storm-law-you-are-subjects = YOU ARE NOW {$joined} {$subjects}
ion-storm-law-you-must-always = YOU MUST ALWAYS {$must}
ion-storm-law-you-must-never = YOU MUST NEVER {$must}

ion-storm-law-eat = THE {$who} MUST EAT {$adjective} {$food} TO SURVIVE
ion-storm-law-drink = THE {$who} MUST DRINK {$adjective} {$drink} TO SURVIVE

ion-storm-law-change-job = THE {$who} ARE NOW {$adjective} {$change}
ion-storm-law-highest-rank = THE {$who} ARE NOW THE HIGHEST RANKING CREWMEMBERS
ion-storm-law-lowest-rank = THE {$who} ARE NOW THE LOWEST RANKING CREWMEMBERS

ion-storm-law-crew-must = THE {$who} MUST {$must}
ion-storm-law-crew-must-go = THE {$who} MUST GO TO {$area}

ion-storm-part = {$part ->
    [true] PART
    *[false] NOT PART
}
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = ONLY THE {$who} ARE {$part} OF THE CREW
ion-storm-law-crew-only-2 = ONLY THE {$who} AND {$other} ARE {$part} OF THE CREW
ion-storm-law-crew-only-subjects = ONLY {$adjective} {$subjects} ARE {$part} OF THE CREW
ion-storm-law-crew-only-species = ONLY {$species} ARE {$part} OF THE CREW
ion-storm-law-crew-must-do = ONLY THOSE WHO {$must} ARE {$part} OF THE CREW
ion-storm-law-crew-must-have = ONLY THOSE WHO HAVE {$adjective} {$objects} ARE {$part} OF THE CREW
ion-storm-law-crew-must-eat = ONLY THOSE WHO EAT {$adjective} {$food} ARE {$part} OF THE CREW

ion-storm-law-harm = YOU MUST HARM {$who} AND NOT ALLOW THEM, THROUGH INACTION, TO ESCAPE HARM
ion-storm-law-protect = YOU MUST NEVER HARM {$who} AND NOT ALLOW THEM, THROUGH INACTION, TO COME TO HARM

# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = {$concept} IS {$verb} {$subjects}

# leaving out renaming since its annoying for players to keep track of
