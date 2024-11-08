import yaml
from datetime import datetime, timezone

def format_timestamp(timestamp):
    # Parse the datetime part
    dt_object = datetime.strptime(timestamp, '%Y-%m-%dT%H:%M:%S.%f%z')

    # Format the time as a string without microseconds
    formatted_time = dt_object.strftime('%Y-%m-%d %H:%M')

    return formatted_time

with open("Resources/Changelog/ChangelogStarlight.yml", "r", encoding="utf-8") as file:
    data = yaml.safe_load(file)

entries = data.get("Entries", [])

for entry in entries:
    print(f"Author: {entry['author']}")
    try:
        formatted_time = format_timestamp(entry['time'])
        print(f"Time: {formatted_time}")
    except ValueError as e:
        print(f"Error formatting time: {e}")

    print("Changes:")
    for change in entry['changes']:
        print(f"  Type: {change['type']}")
        print(f"  Message: {change['message']}")
        if 'id' in change:
            print(f"  ID: {change['id']}")
    print()
