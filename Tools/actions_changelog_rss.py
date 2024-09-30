#!/usr/bin/env python3

#
# Updates an RSS file on a remote server with updates to the changelog.
# See https://docs.spacestation14.io/en/hosting/changelogs for instructions.
#

# If you wanna test this script locally on Windows,
# you can use something like this in Powershell to set up the env var:
# $env:CHANGELOG_RSS_KEY=[System.IO.File]::ReadAllText($(gci "key"))

import os
import paramiko
import pathlib
import io
import base64
import yaml
import itertools
import html
import email.utils
from typing import  List, Any, Tuple
from lxml import etree as ET
from datetime import datetime, timedelta, timezone

MAX_ITEM_AGE = timedelta(days=30)

# Set as a repository secret.
CHANGELOG_RSS_KEY = os.environ.get("CHANGELOG_RSS_KEY")

# Change these to suit your server settings
# https://docs.fabfile.org/en/stable/getting-started.html#run-commands-via-connections-and-run
SSH_HOST = "moon.spacestation14.com"
SSH_USER = "changelog-rss"
SSH_PORT = 22
RSS_FILE = "changelog.xml"
XSL_FILE = "stylesheet.xsl"
HOST_KEYS = [
    "AAAAC3NzaC1lZDI1NTE5AAAAIOBpGO/Qc6X0YWuw7z+/WS/65+aewWI29oAyx+jJpCmh"
]

# RSS feed parameters, change these
FEED_TITLE       = "Space Station 14 Changelog"
FEED_LINK        = "https://github.com/space-wizards/space-station-14/"
FEED_DESCRIPTION = "Changelog for the official Wizard's Den branch of Space Station 14."
FEED_LANGUAGE    = "en-US"
FEED_GUID_PREFIX = "ss14-changelog-wizards-"
FEED_URL         = "https://central.spacestation14.io/changelog.xml"

CHANGELOG_FILE = "Resources/Changelog/Changelog.yml"

TYPES_TO_EMOJI = {
    "Fix":    "🐛",
    "Add":    "🆕",
    "Remove": "❌",
    "Tweak":  "⚒️"
}

XML_NS = "https://spacestation14.com/changelog_rss"
XML_NS_B = f"{{{XML_NS}}}"

XML_NS_ATOM = "http://www.w3.org/2005/Atom"
XML_NS_ATOM_B = f"{{{XML_NS_ATOM}}}"

ET.register_namespace("ss14", XML_NS)
ET.register_namespace("atom", XML_NS_ATOM)

# From https://stackoverflow.com/a/37958106/4678631
class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if not 'yaml_implicit_resolvers' in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()

        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [(tag, regexp)
                                                         for tag, regexp in mappings
                                                         if tag != tag_to_remove]

# Hrm yes let's make the fucking default of our serialization library to PARSE ISO-8601
# but then output garbage when re-serializing.
NoDatesSafeLoader.remove_implicit_resolver('tag:yaml.org,2002:timestamp')

def main():
    if not CHANGELOG_RSS_KEY:
        print("::notice ::CHANGELOG_RSS_KEY not set, skipping RSS changelogs")
        return

    with open(CHANGELOG_FILE, "r") as f:
        changelog = yaml.load(f, Loader=NoDatesSafeLoader)

    with paramiko.SSHClient() as client:
        load_host_keys(client.get_host_keys())
        client.connect(SSH_HOST, SSH_PORT, SSH_USER, pkey=load_key(CHANGELOG_RSS_KEY))
        sftp = client.open_sftp()

        last_feed_items = load_last_feed_items(sftp)

        feed, any_new = create_feed(changelog, last_feed_items)

        if not any_new:
            print("No changes since last last run.")
            return

        et = ET.ElementTree(feed)
        with sftp.open(RSS_FILE, "wb") as f:
            et.write(
                f,
                encoding="utf-8",
                xml_declaration=True,
                # This ensures our stylesheet is loaded
                doctype="<?xml-stylesheet type='text/xsl' href='./stylesheet.xsl'?>",
            )

        # Copy in the stylesheet
        dir_name = os.path.dirname(__file__)

        template_path = pathlib.Path(dir_name, 'changelogs', XSL_FILE)
        with sftp.open(XSL_FILE, "wb") as f, open(template_path) as fh:
            f.write(fh.read())


def create_feed(changelog: Any, previous_items: List[Any]) -> Tuple[Any, bool]:
    rss = ET.Element("rss", attrib={"version": "2.0"})
    channel = ET.SubElement(rss, "channel")

    time_now = datetime.now(timezone.utc)

    # Fill out basic channel info
    ET.SubElement(channel, "title").text       = FEED_TITLE
    ET.SubElement(channel, "link").text        = FEED_LINK
    ET.SubElement(channel, "description").text = FEED_DESCRIPTION
    ET.SubElement(channel, "language").text    = FEED_LANGUAGE

    ET.SubElement(channel, "lastBuildDate").text = email.utils.format_datetime(time_now)
    ET.SubElement(channel, XML_NS_ATOM_B + "link", {"type": "application/rss+xml", "rel": "self", "href": FEED_URL})

    # Find the last item ID mentioned in the previous changelog
    last_changelog_id = find_last_changelog_id(previous_items)

    any = create_new_item_since(changelog, channel, last_changelog_id, time_now)
    copy_previous_items(channel, previous_items, time_now)

    return rss, any

def create_new_item_since(changelog: Any, channel: Any, since: int, now: datetime) -> bool:
    entries_for_item = [entry for entry in changelog["Entries"] if entry["id"] > since]
    top_entry_id = max(map(lambda e: e["id"], entries_for_item), default=0)

    if not entries_for_item:
        return False

    attrs = {XML_NS_B + "from-id": str(since), XML_NS_B + "to-id": str(top_entry_id)}
    new_item = ET.SubElement(channel, "item", attrs)
    ET.SubElement(new_item, "pubDate").text = email.utils.format_datetime(now)
    ET.SubElement(new_item, "guid", {"isPermaLink": "false"}).text = f"{FEED_GUID_PREFIX}{since}-{top_entry_id}"

    ET.SubElement(new_item, "description").text = generate_description_for_entries(entries_for_item)

    # Embed original entries inside the XML so it can be displayed more nicely by specialized tools.
    # Like the website!
    for entry in entries_for_item:
        xml_entry = ET.SubElement(new_item, XML_NS_B + "entry")
        ET.SubElement(xml_entry, XML_NS_B + "id").text = str(entry["id"])
        ET.SubElement(xml_entry, XML_NS_B + "time").text = entry["time"]
        ET.SubElement(xml_entry, XML_NS_B + "author").text = entry["author"]

        for change in entry["changes"]:
            attrs = {XML_NS_B + "type": change["type"]}
            ET.SubElement(xml_entry, XML_NS_B + "change", attrs).text = change["message"]

    return True

def generate_description_for_entries(entries: List[Any]) -> str:
    desc = io.StringIO()

    keyfn = lambda x: x["author"]
    sorted_author = sorted(entries, key=keyfn)
    for author, group in itertools.groupby(sorted_author, keyfn):
        desc.write(f"<h3>{html.escape(author)} updated:</h3>\n")
        desc.write("<ul>\n")
        for entry in sorted(group, key=lambda x: x["time"]):
            for change in entry["changes"]:
                emoji = TYPES_TO_EMOJI.get(change["type"], "")
                msg = change["message"]
                desc.write(f"<li>{emoji} {html.escape(msg)}</li>")

        desc.write("</ul>\n")

    return desc.getvalue()

def copy_previous_items(channel: Any, previous: List[Any], now: datetime):
    # Copy in previous items, if we have them.
    for item in previous:
        date_elem = item.find("./pubDate")
        if date_elem is None:
            # Item doesn't have a valid publication date?
            continue

        date = email.utils.parsedate_to_datetime(date_elem.text or "")
        if date + MAX_ITEM_AGE < now:
            # Item too old, get rid of it.
            continue

        channel.append(item)

def find_last_changelog_id(items: List[Any]) -> int:
    return max(map(lambda i: int(i.get(XML_NS_B + "to-id", "0")), items), default=0)

def load_key(key_contents: str) -> paramiko.PKey:
    key_string = io.StringIO()
    key_string.write(key_contents)
    key_string.seek(0)
    return paramiko.Ed25519Key.from_private_key(key_string)


def load_host_keys(host_keys: paramiko.HostKeys):
    for key in HOST_KEYS:
        host_keys.add(SSH_HOST, "ssh-ed25519", paramiko.Ed25519Key(data=base64.b64decode(key)))


def load_last_feed_items(client: paramiko.SFTPClient) -> List[Any]:
    try:
        with client.open(RSS_FILE, "rb") as f:
            feed = ET.parse(f)

        return feed.findall("./channel/item")

    except FileNotFoundError:
        return []



main()
