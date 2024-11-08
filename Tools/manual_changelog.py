#!/usr/bin/python
# Update the changelog manually, for when you don't want to bother setting up the bot.

import argparse
import datetime
from re import compile as re_compile, I as re_I
from sys import stdin
from yaml import safe_load as yaml_safe_load, dump as yaml_dump


def make_timestamp():
    now = datetime.datetime.now(datetime.timezone.utc)
    return now.isoformat()

def load_changelog(infile):
    print(f"Loading changelog {infile.name} ...")
    return yaml_safe_load(infile)

def make_change(change_type, message):
    change_type = change_type.lower().capitalize()

    assert(type(message) == str)
    assert(change_type in ['Add', 'Remove', 'Fix', 'Tweak'])

    return {
        'message': message,
        'type': change_type,
    }

def get_last_id(changelog):
    return changelog['Entries'][-1]['id']

def insert_entry(changelog, author, changes):
    new_id = 1 + get_last_id(changelog)

    assert(type(author) == str)

    entry = {
        'author': author,
        'changes': changes,
        'id': new_id,
        'time': make_timestamp(),
    }

    changelog['Entries'].append(entry)

def prune_entries(changelog, max_entries=500):
    print(f"Pruning changelog to a maxmimum of {max_entries} entries ...")
    changelog['Entries'] = changelog['Entries'][-max_entries:]

def save_changelog(changelog, outfile):
    print(f"Saving changelog to {outfile.name} ...")
    yaml_dump(changelog, outfile)

def parse_github_pull_request(changelog, stream):
    re_changelog_header = re_compile(r'^\s*:?cl:?\s*(?P<author>.*)', re_I)
    re_change = re_compile(r'^\s*-?\s*(?P<type>add|fix|remove|tweak):(?P<message>.+)', re_I)

    pending_entries_with_unspecified_author = []
    author = ''
    stored_changes = []

    def flush_changes(changes):
        if len(changes) > 0:
            if author == '':
                pending_entries_with_unspecified_author.append(changes[:])
            else:
                insert_entry(changelog, author, changes[:])

            changes[:] = []

    print("Type or paste in changelog entries from GitHub.\n")

    try:
        # Parse the stream.
        for line in stream:
            line = line.strip()

            # Read change lines.
            match = re_change.match(line)

            if match:
                group = match.groupdict()

                ctype = group['type'].strip()
                cmessage = group['message'].strip()

                stored_changes.append(make_change(
                    ctype,
                    cmessage,
                ))
                continue

            # Read author lines.
            match = re_changelog_header.match(line)

            if match:
                # Flush any changes before setting the new author.
                flush_changes(stored_changes)

                group = match.groupdict()
                author = group['author'].strip()
    except KeyboardInterrupt:
        pass

    # Finished reading from stream.

    # Flush any changes.
    flush_changes(stored_changes)

    print("")

    # Deal with unspecified authors.
    for entry in pending_entries_with_unspecified_author:
        print(f"[!] Entry with unspecified author:\n{entry}")
        print("\nEnter author> ", end='')
        author = input()
        insert_entry(changelog, author, entry)


def main():
    default_filename = 'Resources/Changelog/ChangelogStarlight.yml'

    parser = argparse.ArgumentParser(description='Update the changelog manually.')

    parser.add_argument('--infile', type=str,
                        default=default_filename,
                        help='which file to load current entries from')

    parser.add_argument('--outfile', type=str,
                        default=default_filename,
                        help='which file to save results to')

    args = parser.parse_args()

    infile = open(args.infile, 'r', encoding="utf-8-sig")
    cl = load_changelog(infile)
    infile.close()

    parse_github_pull_request(cl, stdin)
    prune_entries(cl)

    outfile = open(args.outfile, 'w', encoding="utf-8-sig")
    save_changelog(cl, outfile)
    outfile.close()

    print("Done!")

if __name__ == "__main__":
    main()
