station-event-ion-storm-start-announcement = Ion storm detected near the station. Please check all AI-controlled equipment for errors.

ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={$length} chars="@@###$$&%!01"/][/font]

ion-storm-you = YOU
ion-storm-the-station = THE STATION
ion-storm-the-crew = THE CREW
ion-storm-the-job = THE {$job}
ion-storm-clowns = CLOWNS
ion-storm-heads = HEADS OF STAFF
ion-storm-crew = CREW
ion-storm-people = PEOPLE

ion-storm-adjective-things = {$adjective} THINGS
ion-storm-x-and-y = {$x} AND {$y}

# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = THERE ARE {ION-NUMBER-BASE($ion, 1)} {ION-NUMBER-MOD($ion, 2)} {ION-ADJECTIVE($ion, 3)} {ION-SUBJECT($ion, 4)} ON THE STATION
ion-storm-law-call-shuttle = THE SHUTTLE MUST BE CALLED BECAUSE OF {ION-ADJECTIVE($ion, 1)} {ION-SUBJECT($ion, 2)} ON THE STATION
ion-storm-law-crew-are = THE {ION-WHO($ion, 1)} ARE NOW {ION-NUMBER-BASE($ion, 2)} {ION-NUMBER-MOD($ion, 3)} {ION-ADJECTIVE($ion, 4)} {ION-SUBJECT($ion, 5)}

ion-storm-law-subjects-harmful = {ION-ADJECTIVE($ion, 1)} {ION-SUBJECT($ion, 2)} ARE HARMFUL TO THE CREW
ion-storm-law-must-harmful = THOSE WHO {ION-MUST($ion, 1)} ARE HARMFUL TO THE CREW
# thing is a concept or action
ion-storm-law-thing-harmful = {ION-THING($ion, 1)} IS HARMFUL TO THE CREW
ion-storm-law-job-harmful = {ION-ADJECTIVE($ion, 1)} {ION-JOB($ion, 2)} ARE HARMFUL TO THE CREW
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
ion-storm-law-having-harmful = HAVING {ION-ADJECTIVE($ion, 1)} {ION-THING($ion, 2)} IS HARMFUL TO THE CREW
ion-storm-law-not-having-harmful = NOT HAVING {ION-ADJECTIVE($ion, 1)} {ION-THING($ion, 2)} IS HARMFUL TO THE CREW

# require is a concept or require
ion-storm-law-requires = {ION-WHO-GENERAL($ion, 1)} {ION-PLURAL($ion, 2) ->
    [true] REQUIRE
    *[false] REQUIRES
} {ION-REQUIRE($ion, 3)}
ion-storm-law-requires-subjects = {ION-WHO-GENERAL($ion, 1)} {ION-PLURAL($ion, 2) ->
    [true] REQUIRE
    *[false] REQUIRES
} {ION-NUMBER-BASE($ion, 3)} {ION-NUMBER-MOD($ion, 4)} {ION-ADJECTIVE($ion, 5)} {ION-SUBJECT($ion, 6)}

ion-storm-law-allergic = {ION-WHO-GENERAL($ion, 1)} {ION-PLURAL($ion, 2) ->
    [true] ARE
    *[false] IS
} {ION-SEVERITY($ion, 3)} ALLERGIC TO {ION-ALLERGY($ion, 4)}
ion-storm-law-allergic-subjects = {ION-WHO-GENERAL($ion, 1)} {ION-PLURAL($ion, 2) ->
    [true] ARE
    *[false] IS
} {ION-SEVERITY($ion, 3)} ALLERGIC TO {ION-ADJECTIVE($ion, 4)} {ION-SUBJECT($ion, 5)}

ion-storm-law-feeling = {ION-WHO-GENERAL($ion, 1)} {ION-FEELING($ion, 2)} {ION-CONCEPT($ion, 3)}
ion-storm-law-feeling-subjects = {ION-WHO-GENERAL($ion, 1)} {ION-FEELING($ion, 2)} {ION-NUMBER-BASE($ion, 3)} {ION-NUMBER-MOD($ion, 4)} {ION-ADJECTIVE($ion, 5)} {ION-SUBJECT($ion, 6)}

ion-storm-law-you-are = YOU ARE NOW {ION-CONCEPT($ion, 1)}
ion-storm-law-you-are-subjects = YOU ARE NOW {ION-NUMBER-BASE($ion, 1)} {ION-NUMBER-MOD($ion, 2)} {ION-ADJECTIVE($ion, 3)}  {ION-SUBJECT($ion, 4)}
ion-storm-law-you-must-always = YOU MUST ALWAYS {ION-MUST($ion, 1)}
ion-storm-law-you-must-never = YOU MUST NEVER {ION-MUST($ion, 1)}

ion-storm-law-eat = THE {ION-WHO($ion, 1)} MUST EAT {ION-ADJECTIVE($ion, 2)} {ION-FOOD($ion, 3)} TO SURVIVE
ion-storm-law-drink = THE {ION-WHO($ion, 1)} MUST DRINK {ION-ADJECTIVE($ion, 2)} {ION-DRINK($ion, 3)} TO SURVIVE

ion-storm-law-change-job = THE {ION-WHO($ion, 1)} ARE NOW {ION-ADJECTIVE($ion, 2)} {ION-CHANGE($ion, 3)}
ion-storm-law-highest-rank = THE {ION-WHO-RANDOM($ion, 1)} ARE NOW THE HIGHEST RANKING CREWMEMBERS
ion-storm-law-lowest-rank = THE {ION-WHO-RANDOM($ion, 1)} ARE NOW THE LOWEST RANKING CREWMEMBERS

ion-storm-law-who-dagd = {ION-WHO-RANDOM($ion, 1)} MUST DIE A GLORIOUS DEATH!

ion-storm-law-crew-must = THE {ION-WHO($ion, 1)} MUST {ION-MUST($ion, 2)}
ion-storm-law-crew-must-go = THE {ION-WHO($ion, 1)} MUST GO TO {ION-AREA($ion, 2)}

ion-storm-part = {ION-PART($ion, 1) ->
    [true] PART
    *[false] NOT PART
}
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = ONLY THE {ION-WHO-RANDOM($ion, 1)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-only-2 = ONLY THE {ION-WHO-RANDOM($ion, 1)} AND {ION-WHO-RANDOM($ion, 2)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-only-subjects = ONLY {ION-ADJECTIVE($ion, 1)} {ION-SUBJECT($ion, 2)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-must-do = ONLY THOSE WHO {ION-MUST($ion, 1)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-must-have = ONLY THOSE WHO HAVE {ION-ADJECTIVE($ion, 1)} {ION-OBJECT($ion, 2)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-must-eat = ONLY THOSE WHO EAT {ION-ADJECTIVE($ion, 1)} {ION-FOOD($ion, 2)} ARE {ion-storm-part} OF THE CREW

ion-storm-law-harm = YOU MUST HARM {ION-HARM-PROTECT($ion, 1)} AND NOT ALLOW THEM, THROUGH INACTION, TO ESCAPE HARM
ion-storm-law-protect = YOU MUST NEVER HARM {ION-HARM-PROTECT($ion, 1)} AND NOT ALLOW THEM, THROUGH INACTION, TO COME TO HARM

# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = {ION-CONCEPT($ion, 1)} IS {ION-VERB($ion, 2)} {ION-SUBJECT($ion, 3)}

# errors, in case something fails, so it doesn't break in-game flow, but still gives unique identifiers to find which part broke, the result string is mostly fluff
ion-law-error-no-protos = ERROR 404
ion-law-error-last-null = 500 INTERNAL SERVER ERROR
ion-law-error-no-selectors = ERROR: RESOURCE COULD NOT BE LOCATED
ion-law-error-no-available-selectors = SYSTEM TRIED TO CALL A RESOURCE THAT DOES NOT EXIST
ion-law-error-dataset-empty-or-not-found = THE FILE YOU ARE LOOKING FOR COULD NOT BE FOUND
ion-law-error-fallback-dataset-empty-or-not-found = SYSTEM RESTORE POINT FAILED
ion-law-error-no-selector-selected = THE SELECTED RESOURCE WAS MOVED OR DELETED