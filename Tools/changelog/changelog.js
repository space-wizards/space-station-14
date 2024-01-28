// Dependencies
const fs = require("fs");
const yaml = require("js-yaml");
const axios = require("axios");

// Use GitHub token if available
if (process.env.GITHUB_TOKEN) axios.defaults.headers.common["Authorization"] = `Bearer ${process.env.GITHUB_TOKEN}`;

// Regexes
const HeaderRegex = /^\s*(?::cl:|🆑) *([a-z0-9_\- ]+)?\s+/im; // :cl: or 🆑 [0] followed by optional author name [1]
const EntryRegex = /^ *[*-]? *(add|remove|tweak|fix): *([^\n\r]+)\r?$/img; // * or - followed by change type [0] and change message [1]
const CommentRegex = /<!--.*?-->/gs; // HTML comments

// Main function
async function main() {
    // Get PR details
    const pr = await axios.get(`https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/pulls/${process.env.PR_NUMBER}`);
    const { merged_at, body, user } = pr.data;

    // Remove comments from the body
    commentlessBody = body.replace(CommentRegex, '');

    // Get author
    const headerMatch = HeaderRegex.exec(commentlessBody);
    if (!headerMatch) {
        console.log("No changelog entry found, skipping");
        return;
    }

    let author = headerMatch[1];
    if (!author) {
        console.log("No author found, setting it to author of the PR\n");
        author = user.login;
    }

    // Get all changes from the body
    const entries = getChanges(commentlessBody);


    // Time is something like 2021-08-29T20:00:00Z
    // Time should be something like 2023-02-18T00:00:00.0000000+00:00
    let time = merged_at;
    if (time)
    {
        time = time.replace("z", ".0000000+00:00").replace("Z", ".0000000+00:00");
    }
    else
    {
        console.log("Pull request was not merged, skipping");
        return;
    }


    // Construct changelog yml entry
    const entry = {
        author: author,
        changes: entries,
        id: getHighestCLNumber() + 1,
        time: time,
    };

    // Write changelogs
    writeChangelog(entry);

    console.log(`Changelog updated with changes from PR #${process.env.PR_NUMBER}`);
}


// Code chunking

// Get all changes from the PR body
function getChanges(body) {
    const matches = [];
    const entries = [];

    for (const match of body.matchAll(EntryRegex)) {
        matches.push([match[1], match[2]]);
    }

    if (!matches)
    {
        console.log("No changes found, skipping");
        return;
    }


    // Check change types and construct changelog entry
    matches.forEach((entry) => {
        let type;

        switch (entry[0].toLowerCase()) {
            case "add":
                type = "Add";
                break;
            case "remove":
                type = "Remove";
                break;
            case "tweak":
                type = "Tweak";
                break;
            case "fix":
                type = "Fix";
                break;
            default:
                break;
        }

        if (type) {
            entries.push({
                type: type,
                message: entry[1],
            });
        }
    });

    return entries;
}

// Get the highest changelog number from the changelogs file
function getHighestCLNumber() {
    // Read changelogs file
    const file = fs.readFileSync(`../../${process.env.CHANGELOG_DIR}`, "utf8");

    // Get list of CL numbers
    const data = yaml.load(file);
    const entries = data && data.Entries ? Array.from(data.Entries) : [];
    const clNumbers = entries.map((entry) => entry.id);

    // Return highest changelog number
    return Math.max(...clNumbers, 0);
}

function writeChangelog(entry) {
    let data = { Entries: [] };

    // Create a new changelogs file if it does not exist
    if (fs.existsSync(`../../${process.env.CHANGELOG_DIR}`)) {
        const file = fs.readFileSync(`../../${process.env.CHANGELOG_DIR}`, "utf8");
        data = yaml.load(file);
    }

    data.Entries.push(entry);

    // Write updated changelogs file
    fs.writeFileSync(
        `../../${process.env.CHANGELOG_DIR}`,
        "Entries:\n" +
            yaml.dump(data.Entries, { indent: 2 }).replace(/^---/, "")
    );
}

// Run main
main();
