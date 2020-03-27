Changelogs are included with commits as text .yml files created individually by the committer. If you want to create a changelog entry you create a .yml file in the /changelogs directory; nothing else needs to be touched unless you are a maintainer.

#######################################################

TO MAKE A CHANGELOG .YML ENTRRY

0. Consider carefully if a change you make really needs a changelog, and if it does how many lines should be dedicated to it. A set of changes with no forward facing player effects like a refactor almost certainly doesn't need a changelog, nor does something that makes itself obvious during the normal course of play like adding an action button to an item players already commonly pick up. Likewise if you DO need a changelog consider packaging similar changes under a generalized line instead of listing out every little change as its own thing. Only you can prevent changelog clutter.

1. Make a copy of the file example.yml in html/changelogs and rename it to [YOUR USERNAME]-PR-[YOUR PR NUMBER].yml (the pr and pr number are organizational and can be ignored if you so wish)

2. Change the author to yourself

3. Replace the changes text with a description of the changes in your PR, keep the double quotes to avoid errors (your changelog can be written ICly or OOCly, it doesn't matter)

4. (Optional) set the change prefix (rscadd) to a different one listed above in example.yml (this affects what icon is used for your changelog entry)

5. When commiting make sure your .yml file is included in the commit (it will usually be unticked as an unversioned file)

#######################################################

If you have trouble ask for help in #coderbus or read https://tgstation13.org/wiki/Guide_to_Changelogs
