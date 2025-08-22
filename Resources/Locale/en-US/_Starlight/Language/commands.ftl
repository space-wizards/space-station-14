command-list-langs-desc = List languages your current entity can speak at the current moment.
command-list-langs-help = Usage: {$command}
command-saylang-desc = Send a message in a specific language. To choose a language, you can use either the name of the language, or its position in the list of languages.
command-saylang-help = Usage: {$command} <language id> <message>. Example: {$command} GalacticCommon "Hello World!". Example: {$command} 1 "Hello World!"
command-language-select-desc = Select the currently spoken language of your entity. You can use either the name of the language, or its position in the list of languages.
command-language-select-help = Usage: {$command} <language id>. Example: {$command} 1. Example: {$command} GalacticCommon
command-language-spoken = Spoken:
command-language-understood = Understood:
command-language-current-entry = {$id}. {$language} - {$name} (current)
command-language-entry = {$id}. {$language} - {$name}
command-language-invalid-number = The language number must be between 0 and {$total}. Alternatively, use the language name.
command-language-invalid-language = The language {$id} does not exist or you cannot speak it.
# Toolshed

command-description-language-add = Adds a new language to the piped entity. The two last arguments indicate whether it should be spoken/understood. Example: 'self language:add "Canilunzt" true true'
command-description-language-rm = Removes a language from the piped entity. Works similarly to language:add. Example: 'self language:rm "GalacticCommon" true true'.
command-description-language-lsspoken = Lists all languages the entity can speak. Example: 'self language:lsspoken'
command-description-language-lsunderstood = Lists all languages the entity can understand. Example: 'self language:lssunderstood'
command-description-translator-addlang = Adds a new target language to the piped translator entity. See language:add for details.
command-description-translator-rmlang = Removes a target language from the piped translator entity. See language:rm for details.
command-description-translator-addrequired = Adds a new required language to the piped translator entity. Example: 'ent 1234 translator:addrequired "GalacticCommon"'
command-description-translator-rmrequired = Removes a required language from the piped translator entity. Example: 'ent 1234 translator:rmrequired "GalacticCommon"'
command-description-translator-lsspoken = Lists all spoken languages for the piped translator entity. Example: 'ent 1234 translator:lsspoken'
command-description-translator-lsunderstood = Lists all understood languages for the piped translator entity. Example: 'ent 1234 translator:lssunderstood'
command-description-translator-lsrequired = Lists all required languages for the piped translator entity. Example: 'ent 1234 translator:lsrequired'
command-language-error-this-will-not-work = This will not work.
command-language-error-not-a-translator = Entity {$entity} is not a translator.