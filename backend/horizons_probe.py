"""First Lunar Navigator science check: JPL Horizons azimuth/elevation.

Usage:
  python -m pip install requests
  python horizons_probe.py --lon 0 --lat -89.5 --start 2026-12-01 --days 3 --step-hours 1

The default coordinates are a test point, not a vetted landing site.
Sun/Earth visibility here is GEOMETRIC HORIZON ONLY; terrain is not considered.
"""

import argparse
import csv
from datetime import datetime, timedelta
from pathlib import Path

import requests

API = "https://ssd.jpl.nasa.gov/api/horizons.api"


def fetch(target_id: int, lon: float, lat: float, start: str, stop: str, step_hours: int):
    params = {
        "format": "json",
        "COMMAND": f"'{target_id}'",
        "OBJ_DATA": "'NO'",
        "MAKE_EPHEM": "'YES'",
        "EPHEM_TYPE": "'OBSERVER'",
        "CENTER": "'coord@301'",  # topocentric observation site on the Moon
        "COORD_TYPE": "'GEODETIC'",
        "SITE_COORD": f"'{lon},{lat},0'",  # longitude E, latitude, reference altitude km
        "START_TIME": f"'{start}'",
        "STOP_TIME": f"'{stop}'",
        "STEP_SIZE": f"'{step_hours} h'",
        "QUANTITIES": "'4'",  # apparent azimuth and elevation in degrees
        "CSV_FORMAT": "'YES'",
        "APPARENT": "'AIRLESS'",
        "ANG_FORMAT": "'DEG'",
        "TIME_DIGITS": "'MINUTES'",
    }
    response = requests.get(API, params=params, timeout=90)
    response.raise_for_status()
    result = response.json().get("result", "")
    if "$$SOE" not in result or "$$EOE" not in result:
        raise RuntimeError(f"Horizons did not return a data table:\n{result[:2200]}")

    records = []
    block = result.split("$$SOE", 1)[1].split("$$EOE", 1)[0]
    for row in csv.reader(block.strip().splitlines()):
        if not row or not row[0].strip():
            continue
        time = row[0].strip()
        # Quantity 4 is the only requested numeric quantity: azimuth and elevation.
        numbers = []
        for cell in row[1:]:
            try:
                numbers.append(float(cell.strip()))
            except ValueError:
                pass
        if len(numbers) != 2:
            raise RuntimeError(f"Unexpected Horizons row ({len(numbers)} numbers): {row!r}")
        az, el = numbers
        if not (0 <= az <= 360 and -90 <= el <= 90):
            raise RuntimeError(f"Unexpected angle values in row: {row!r}")
        records.append({"utc": time, "az_deg": az, "el_deg": el})
    if not records:
        raise RuntimeError("Horizons returned zero ephemeris rows")
    return records, result


def write_csv(path, fieldnames, rows):
    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--lon", type=float, default=0.0, help="Lunar longitude in degrees east")
    parser.add_argument("--lat", type=float, default=-89.5, help="Lunar latitude, south is negative")
    parser.add_argument("--start", default="2026-12-01", help="First UTC date YYYY-MM-DD")
    parser.add_argument("--days", type=int, default=3)
    parser.add_argument("--step-hours", type=int, default=1)
    parser.add_argument("--out", type=Path, default=Path("horizons_output"))
    args = parser.parse_args()
    if not (-90 <= args.lat <= 90 and -360 <= args.lon <= 360):
        parser.error("Latitude must be [-90, 90]; longitude must be [-360, 360]")
    if args.days < 1 or args.step_hours < 1:
        parser.error("Days and step-hours must be positive integers")

    stop = (datetime.strptime(args.start, "%Y-%m-%d") + timedelta(days=args.days)).strftime("%Y-%m-%d")
    args.out.mkdir(parents=True, exist_ok=True)
    results = {}
    for name, target_id in (("sun", 10), ("earth", 399)):
        rows, raw = fetch(target_id, args.lon, args.lat, args.start, stop, args.step_hours)
        results[name] = rows
        (args.out / f"horizons_{name}_raw.txt").write_text(raw, encoding="utf-8")
        write_csv(args.out / f"{name}.csv", ("utc", "az_deg", "el_deg"), rows)
        print(f"{name}: {len(rows)} rows, first row: {rows[0]}")

    sun = {r["utc"]: r for r in results["sun"]}
    earth = {r["utc"]: r for r in results["earth"]}
    times = sorted(set(sun) & set(earth))
    if not times:
        raise RuntimeError("Sun/Earth response time columns do not overlap")
    output = []
    for t in times:
        s, e = sun[t], earth[t]
        output.append({
            "utc": t,
            "sun_az_deg": s["az_deg"], "sun_el_deg": s["el_deg"],
            "earth_az_deg": e["az_deg"], "earth_el_deg": e["el_deg"],
            "sun_above_GEOMETRIC_horizon": int(s["el_deg"] > 0),
            "earth_above_GEOMETRIC_horizon": int(e["el_deg"] > 0),
        })
    write_csv(args.out / "combined_geometric_only.csv", list(output[0]), output)
    print(f"Saved {len(output)} combined timestamps to {args.out / 'combined_geometric_only.csv'}")
    print("WARNING: This does not evaluate terrain obstruction or actual power/comms.")
    print("Horizons: https://ssd-api.jpl.nasa.gov/doc/horizons.html")


if __name__ == "__main__":
    main()
