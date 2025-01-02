import yaml
import os
import sys
from datetime import datetime, timezone

def load_changelog(file_path):
    if not os.path.exists(file_path):
        return {"Entries": []}
    with open(file_path, "r", encoding="utf-8") as file:
        return yaml.safe_load(file) or {"Entries": []}

def save_changelog(file_path, data):
    os.system('cls')
    with open(file_path, "w", encoding="utf-8") as file:
        yaml.dump(data, file, allow_unicode=True, default_flow_style=False)
    print("Changelog saved!")

def display_entries(entries):
    os.system('cls')
    print("\nList of entries:")
    for entry in entries:
        print(f"ID: {entry['id']}, Author: {entry['author']}, Changes: {entry['changes'][0]['type']}")
    input("Press any key to continue...")

def add_entry(entries):
    os.system('cls')
    author = input("Enter author name: ")
    message = input("Enter description of changes: ")

    valid_types = {"Add", "Fix", "Remove", "Tweak"}

    change_type = input("Enter change type (Add, Fix, Remove, or Tweak): ")

    while change_type not in valid_types:
        change_type = input("Enter change type (Add, Fix, Remove, or Tweak): ")

    new_id = max((entry["id"] for entry in entries), default=0) + 1
    new_entry = {
        "author": author,
        "changes": [{"message": message, "type": change_type}],
        "id": new_id,
        "time": datetime.now(timezone.utc).isoformat(timespec='microseconds'),
        "url": input("Enter URL (if any): ")
    }
    entries.append(new_entry)
    print(f"Entry with ID {new_id} added.")

def edit_entry(entries):
    os.system('cls')
    display_entries(entries)
    entry_id = int(input("Enter ID of the entry to edit: "))
    for entry in entries:
        if entry["id"] == entry_id:
            print(f"Current author: {entry['author']}")
            entry["author"] = input("New author (Press Enter to skip): ") or entry["author"]

            print(f"Current message: {entry['changes'][0]['message']}")
            entry["changes"][0]["message"] = input("New message (Press Enter to skip): ") or entry["changes"][0]["message"]

            print(f"Current change type: {entry['changes'][0]['type']}")
            entry["changes"][0]["type"] = input("New change type (Press Enter to skip): ") or entry["changes"][0]["type"]

            print("Entry updated!")
            return
    print("Entry with the specified ID not found.")

def delete_entry(entries):
    os.system('cls')
    display_entries(entries)
    entry_id = int(input("Enter ID of the entry to delete: "))
    for entry in entries:
        if entry["id"] == entry_id:
            entries.remove(entry)
            print(f"Entry with ID {entry_id} deleted.")
            return
    print("Entry with the specified ID not found.")

def main():
    if len(sys.argv) != 2:
        print("Drag and drop a YAML file onto this script to edit it.")
        input("Press any key to continue...")
        sys.exit(1)
    file_path = sys.argv[1]

    changelog = load_changelog(file_path)
    entries = changelog["Entries"]

    while True:
        os.system('cls')
        print("Menu:")
        print("1. View   records")
        print("2. Add    record")
        print("3. Edit   record")
        print("4. Remove record")
        print("5. Save and exit")

        valid_types = {"1", "2", "3", "4", "5"}

        choice = input("Select an action: ")

        while choice not in valid_types:
            os.system('cls')

            print("Menu:")
            print("1. View   records")
            print("2. Add    record")
            print("3. Edit   record")
            print("4. Remove record")
            print("5. Save and exit")
            print(" ")
            print("Invalid choice, please try again.")
            print(" ")
            choice = input("Select an action: ")

        if choice == "1":
            display_entries(entries)
        elif choice == "2":
            add_entry(entries)
        elif choice == "3":
            edit_entry(entries)
        elif choice == "4":
            delete_entry(entries)
        elif choice == "5":
            save_changelog(file_path, changelog)
            break

    input("Press any key to continue...")

if __name__ == "__main__":
   main()
