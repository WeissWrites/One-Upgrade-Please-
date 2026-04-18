"""
Reads WeaponStats.csv and patches every .asset file in this folder
to use the new nested upgradeLevels structure inside each rarity tier.

Run from the Weapons folder:
    python patch_weapons.py
"""

import csv
import os
import re

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CSV_PATH = os.path.join(SCRIPT_DIR, "WeaponStats.csv")

WEAPON_MAP = {
    "AR 1 Modded M4.asset":                          "M4",
    "AR 2 Modded M4-C.asset":                        "M4-C",
    "AR 3 Modded AUG.asset":                         "AUG",
    "AR 4 Modded 552 Commando.asset":                "552 Commando",
    "AR 5 Modded ASH-12.asset":                      "ASH-12",
    "Pistol 1 Modded Hudson H9.asset":               "Hudson H9",
    "Pistol 2 Modded FiveSeven.asset":               "FiveSeven",
    "Pistol 3 Modded Ruger Mark IV 2245 Lite.asset": "Ruger Mark IV",
    "Pistol 4 Modded MicroUzi.asset":                "MicroUzi",
    "Recon 1 Modded CZ 600 Trail.asset":             "CZ 600 Trail",
    "SMG 1 Modded UZI.asset":                        "Uzi",
    "SMG 2 Modded Scorpion EVO.asset":               "Scorpion EVO",
    "SMG 3 Modded P90.asset":                        "P90",
}

# These weapons use the "(Akimbo)" CSV rows for their Legendary rarity
AKIMBO_WEAPONS = {"Hudson H9", "FiveSeven", "Ruger Mark IV", "MicroUzi"}

RARITY_ORDER = ["Common", "Rare", "Epic", "Legendary"]
RARITY_LEVEL = {"Common": 0, "Rare": 1, "Epic": 2, "Legendary": 3}


def load_csv():
    """Returns dict: data[gun_name][rarity][upgrade_level] = row_dict"""
    data = {}
    with open(CSV_PATH, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            name = row["Gun Name"].strip()
            rarity = row["Rarity"].strip()
            level = int(row["Upgrade Level"].strip())
            data.setdefault(name, {}).setdefault(rarity, {})[level] = row
    return data


def parse_attachments(asset_text):
    """Extracts hasSight/hasLaser/hasGrip per rarityName from existing asset."""
    attachments = {}
    # Find each rarity block by rarityName, then grab the three attachment fields
    blocks = re.split(r"(?=  - rarityName:)", asset_text)
    for block in blocks:
        name_match = re.search(r"rarityName: (.+)", block)
        sight_match = re.search(r"hasSight: (\d)", block)
        laser_match = re.search(r"hasLaser: (\d)", block)
        grip_match = re.search(r"hasGrip: (\d)", block)
        if name_match and sight_match:
            rarity_name = name_match.group(1).strip()
            attachments[rarity_name] = {
                "hasSight": int(sight_match.group(1)),
                "hasLaser": int(laser_match.group(1)) if laser_match else 0,
                "hasGrip":  int(grip_match.group(1)) if grip_match else 0,
            }
    return attachments


def format_float(value):
    """Format a float, stripping trailing zeros but keeping precision."""
    f = float(value)
    s = f"{f:.10f}".rstrip("0").rstrip(".")
    return s if s else "0"


def build_rarity_tiers(weapon_name, csv_data, attachments):
    lines = ["  rarityTiers:"]
    for rarity in RARITY_ORDER:
        # Akimbo weapons use a different CSV key for Legendary
        csv_key = weapon_name
        if rarity == "Legendary" and weapon_name in AKIMBO_WEAPONS:
            csv_key = f"{weapon_name} (Akimbo)"

        rarity_rows = csv_data.get(csv_key, {}).get(rarity)
        if not rarity_rows:
            print(f"  WARNING: no data for {weapon_name} / {rarity}, skipping tier")
            continue

        att = attachments.get(rarity, {"hasSight": 0, "hasLaser": 0, "hasGrip": 0})

        lines.append(f"  - rarityName: {rarity}")
        lines.append(f"    rarityLevel: {RARITY_LEVEL[rarity]}")
        lines.append(f"    upgradeLevels:")

        for lvl in range(5):
            row = rarity_rows.get(lvl)
            if not row:
                print(f"  WARNING: missing upgrade level {lvl} for {weapon_name}/{rarity}")
                continue
            lines.append(f"    - upgradeLevel: {lvl}")
            lines.append(f"      damage: {int(row['Damage'])}")
            lines.append(f"      headshotDamage: {int(row['HS Damage'])}")
            lines.append(f"      shootingDelay: {format_float(row['Shooting Delay'])}")
            lines.append(f"      spreadIntensity: {format_float(row['Spread Intensity'])}")
            lines.append(f"      spreadPerShot: {format_float(row['Spread Per Shot'])}")
            lines.append(f"      spreadRecovery: {format_float(row['Spread Recovery'])}")
            lines.append(f"      snappiness: {format_float(row['Snappiness'])}")
            lines.append(f"      returnSpeed: {format_float(row['Return Speed'])}")

        lines.append(f"    hasSight: {att['hasSight']}")
        lines.append(f"    hasLaser: {att['hasLaser']}")
        lines.append(f"    hasGrip: {att['hasGrip']}")

    return "\n".join(lines) + "\n"


def patch_asset(asset_path, weapon_name, csv_data):
    with open(asset_path, "r", encoding="utf-8") as f:
        text = f.read()

    attachments = parse_attachments(text)

    # Split into: header (up to rarityTiers:), rarity block, footer (from fireSound onward)
    rarity_start = text.find("  rarityTiers:")
    footer_start = text.find("  fireSound:")
    if rarity_start == -1 or footer_start == -1:
        print(f"  SKIPPED (unexpected format): {os.path.basename(asset_path)}")
        return

    header = text[:rarity_start]
    footer = text[footer_start:]

    new_rarity_block = build_rarity_tiers(weapon_name, csv_data, attachments)
    new_text = header + new_rarity_block + footer

    with open(asset_path, "w", encoding="utf-8") as f:
        f.write(new_text)

    print(f"  Patched: {os.path.basename(asset_path)}")


def main():
    csv_data = load_csv()
    print(f"Loaded CSV with {sum(len(r) for w in csv_data.values() for r in w.values())} entries\n")

    for filename, weapon_name in WEAPON_MAP.items():
        asset_path = os.path.join(SCRIPT_DIR, filename)
        if not os.path.exists(asset_path):
            print(f"MISSING: {filename}")
            continue
        print(f"Processing {filename} ({weapon_name})...")
        patch_asset(asset_path, weapon_name, csv_data)

    print("\nDone. Reload Unity to see changes.")


if __name__ == "__main__":
    main()
