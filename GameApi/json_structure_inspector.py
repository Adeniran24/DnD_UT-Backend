import json
import sys
import os

def print_structure(data, indent=0, key_name=None):
    prefix = "  " * indent
    key_display = f'"{key_name}": ' if key_name is not None else ""

    if isinstance(data, dict):
        print(f"{prefix}{key_display}Object {{")
        for key, value in data.items():
            print_structure(value, indent + 1, key)
        print(f"{prefix}}}")
    elif isinstance(data, list):
        print(f"{prefix}{key_display}Array [")
        for i, item in enumerate(data[:3]):  # show only first 3 for preview
            print_structure(item, indent + 1)
        if len(data) > 3:
            print(f"{prefix}  ... ({len(data)} total)")
        print(f"{prefix}]")
    elif isinstance(data, str):
        print(f"{prefix}{key_display}string")
    elif isinstance(data, (int, float)):
        print(f"{prefix}{key_display}number")
    elif isinstance(data, bool):
        print(f"{prefix}{key_display}boolean")
    elif data is None:
        print(f"{prefix}{key_display}null")
    else:
        print(f"{prefix}{key_display}{type(data).__name__}")

def main():
    if len(sys.argv) < 2:
        path = input("Enter path to JSON file: ").strip('"')
    else:
        path = sys.argv[1]

    if not os.path.exists(path):
        print(f"❌ File not found: {path}")
        sys.exit(1)

    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        print(f"❌ Error reading JSON: {e}")
        sys.exit(1)

    print("\n=== JSON Structure Inspector ===\n")
    print_structure(data)

if __name__ == "__main__":
    main()
