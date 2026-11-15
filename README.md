# LunarNavigator
### Interactive Lunar South-Pole Landing Site Evaluation

![LunarNavigator Interface Preview](Screenshots/Screenshot%202026-11-15%20225314.png)
*Figure 1: Interface preview in ILLUSTRATIVE DEMO mode. This screenshot shows the layout, 3D globe viewport, scrubbable timeline, and line-of-sight analysis cards—not operational NASA predictions.*

---

## Overview

Planning a landing near the lunar south pole is tricky. The Sun hugs the horizon, shadows stretch for tens of kilometers, and Earth wobbles in and out of direct view. A delay of just a few days can plunge a planned landing site into darkness or kill direct communications.

**LunarNavigator** is an interactive desktop mission-planning tool built for the 2026 NASA Space Apps Challenge (*Lunar Landing Site Navigator*). It joins two systems: a real-time Unity 6 HDRP 3D lunar simulator and a Python 3.11 / FastAPI scientific service. The backend fetches true planetary ephemerides directly from NASA/JPL Horizons, tracks the positions of both the Sun and Earth relative to south-pole landing sites, and calculates visibility metrics across custom mission windows. Planners and students can test dates, spot communication blackouts, inspect sunlit intervals, and rank arrival opportunities directly on an interactive 3D Moon.

---

## Key Features

- **Dynamic Site Inspector**: Pick any supported site, set an exact landing date (UTC), and dial in your mission duration from 1 hour up to 31 days (744 hours).
- **Hard Line-of-Sight Metrics**: Get exact numbers on sunlit percentage, direct-to-Earth visibility, simultaneous Sun + Earth windows, longest continuous blackout, and longest dark period.
- **Landing Opportunity Finder**: Set mission limits—such as requiring at least 70% daylight or capping Earth outages at 6 hours—and let the engine scan arrival dates across a search grid to surface the top qualifying windows.
- **Interactive 3D Moon with 4 Camera Modes**:
  - `Global Orbit`: Spin the entire Moon, zoom in on regional features, and click landing markers.
  - `Cinematic Approach`: Smooth, logarithmic fly-in that carries you from lunar orbit straight to the landing site.
  - `Local Orbit`: Circle around local crater rims, slopes, and ridges.
  - `Surface Observer`: Stand on the surface at a chosen antenna height (2 m default) and watch the local horizon.
- **Three-Track Mission Timeline**: Scrub through time with color-coded tracks for `SUNLIGHT`, `EARTH VISIBLE`, and `BOTH VISIBLE`. Segments display their exact duration in hours right on the track.
- **Curated Reference Centers**: Includes coordinates for high-priority Artemis exploration regions documented by NASA: **Shackleton Rim** (Site 04), **Connecting Ridge** (Site 01), and **Malapert Massif** (Site 23).

---

## How It Works

The system splits into a lightweight frontend and a math-heavy calculation service:

```
Unity 6 (HDRP) Client
  │  (Interactive 3D globe, UI overlays, camera controls, timeline scrubber)
  │
  │  HTTP REST (JSON)
  ▼  POST /analyze | POST /opportunities | GET /sites
FastAPI Scientific Engine (Python 3.11 / Uvicorn)
  │
  ├── Local Run Cache (Instant replay of previously queried windows)
  │
  └── NASA / JPL Horizons API (GET coord@301)
        Target 10: Sun topocentric vector
        Target 399: Earth topocentric vector
```

1. You pick a site and arrival time in the Unity interface, then hit **Analyze Mission Window**.
2. Unity sends an HTTP POST request to the local FastAPI backend.
3. The backend checks its local cache. If the query is new, it calls NASA/JPL Horizons over HTTPS to grab Sun and Earth apparent vectors relative to the Moon's body frame (`coord@301`).
4. The Python engine lines up the ephemeris steps onto a shared UTC timeline, measures geometric elevation against the mean lunar radius ($R_M = 1737.4\text{ km}$), counts consecutive outages, and packages the results.
5. Unity parses the JSON response, points the directional sun light to match the real sky, plots line-of-sight charts, and builds the interactive timeline.

---

## Download and Run

Binary releases are available on GitHub:
👉 **[Download LunarNavigator Windows Standalone](https://github.com/Solayman-Sameh/lunar-navigator/releases)**

> [!IMPORTANT]
> The Windows build is **not** a single-file executable. Extract the entire ZIP archive before launching `LunarNavigator.exe`. Running it from inside the archive will fail to load dependencies.
>
> **The Python backend runs separately.** Because this release does not ship an embedded Python runtime, you must start the backend in a terminal before running analyses in Unity.

### Requirements

- **Operating System**: Windows 10 or 11 (64-bit), graphics card with DirectX 12 support.
- **Python**: Python 3.11 (64-bit) with `pip`.
- **Network**: Internet connection for uncached JPL Horizons requests. Cached mission dates work completely offline.

### Step 1: Start the Scientific Server

Open a terminal (PowerShell or Command Prompt) and set up the backend:

```bash
cd backend

# Create and activate a clean virtual environment
python -m venv .venv
.venv\Scripts\activate

# Install requirements
pip install -r requirements.txt

# Start the server on port 8000
uvicorn science.api:app --host 127.0.0.1 --port 8000
```

To confirm it's running, open `http://127.0.0.1:8000/docs` in your browser. You should see FastAPI's Swagger documentation.

### Step 2: Run the App

1. Unzip `LunarNavigator_Win64.zip`.
2. Double-click `LunarNavigator.exe`.
3. The simulator starts in Full HD and connects to `http://127.0.0.1:8000`.

---

## Quick Start & Controls

1. **Pick a Site**: Choose **Shackleton Rim**, **Connecting Ridge**, or **Malapert Massif** from the dropdown.
2. **Set Touchdown Time**: Enter a UTC date (e.g., `2026-12-01 00:00:00 UTC`) or click `-1d` / `+1d` to shift by days.
3. **Choose Mission Length**: Set how many hours to check (try `72.0` hours).
4. **Run Analysis**: Click **Analyze Mission Window**.
5. **Read the Output**:
   - Check the **Line-of-Sight Metrics** card for daylight and communications coverage.
   - Note the **GEOMETRIC_ONLY** badge on the status display.
   - Drag the timeline scrubber at the bottom to watch the Sun and Earth shift position across the sky.
6. **Compare Options**: Open the **Find Opportunities** tab, set your tolerance for dark periods, and click search to rank potential landing dates.

### Verified Controls

These key bindings are wired directly into the input system:

| Key / Mouse | What It Does |
|---|---|
| `1` | Switch to **Global Orbit** |
| `2` | Fly in with **Cinematic Approach** |
| `3` | Switch to **Local Orbit** |
| `4` | Drop to **Surface Observer** view |
| `Left Drag` | Orbit or pan the camera (ignored when clicking UI) |
| `Left Click` on pin | Select landing site (double-click focuses) |
| `Scroll Wheel` | Zoom in and out |
| `F` | Focus on active site |
| `R` | Reset camera view |
| `Space` | Play or pause timeline clock |
| `Tab` | Hide / show interface overlays |
| `Left` / `Right Arrow` | Step clock backward or forward by 1 hour |
| `Shift` + `Arrow` | Step clock by 6 hours |
| `H` or `/` | Open Help window |
| `Esc` | Close active dialog or back out |

---

## Tech Stack

- **Game Engine**: Unity 6 (`6000.6.3f1`)
- **Graphics Pipeline**: HDRP (`17.6.0`), DirectX 12
- **Scripts**: C# (.NET Standard 2.1, Unity Input System)
- **Backend Service**: Python `3.11`, FastAPI `0.141.1`, Starlette `0.52.1`, Pydantic `2.11.10`
- **ASGI Web Server**: Uvicorn `0.54.0`
- **Testing & Math**: Pytest, NumPy, Matplotlib

---

## NASA Data Sources

- **NASA / JPL Horizons**: Ephemeris data retrieved via the public [Horizons API](https://ssd-api.jpl.nasa.gov/doc/horizons.html). The engine requests topocentric azimuth and elevation for custom lunar observers (`CENTER='coord@301'`).
- **Moon to Mars Lunar Surface Data Book**: Reference centers, site naming, and candidate regions are drawn from [NASA NTRS Report 20250002845](https://ntrs.nasa.gov/citations/20250002845).
- **LROC & LOLA Visual Maps**: Global lunar albedo maps and displacement textures come from the Lunar Reconnaissance Orbiter Camera and LOLA altimeter via the [NASA SVS CGI Moon Kit](https://svs.gsfc.nasa.gov/4720).

---

## Scientific Scope & Limitations

> [!WARNING]
> LunarNavigator is an **environmental planning aid and educational tool**, not flight software.

- **Spherical Horizon Only (`GEOMETRIC_ONLY`)**: Calculations test whether the Sun or Earth sits above the smooth mean lunar sphere ($R_M = 1737.4\text{ km}$). We do not trace rays against local high-resolution DEM horizons in this release. An object showing an elevation of $+0.5^\circ$ is geometrically above the horizon, but in reality a nearby crater wall or mountain could block it.
- **Foreground Terrain is Illustrative**: The 3D surface meshes in the viewer show general topographic context. They do not represent certified landing hazards, rock distributions, or true slope limits.
- **Reference Centers Are Not Touchdown Spots**: Coordinates point to general candidate regions, not certified meter-accurate landing targets.
- **No Power or Radio Budgeting**: Sunlight percentage does not calculate solar panel output, thermal changes, or battery storage. Similarly, Earth visibility does not guarantee an RF link margin or account for antenna gain.

---

## Documentation

Full architectural audits, API specs, and verification records live in the `docs/` folder:

- [docs/SCIENCE_BACKEND_REFERENCE.md](docs/SCIENCE_BACKEND_REFERENCE.md) — Math, reference frames, and ephemeris parsing details.
- [docs/PROJECT_PROPOSAL.md](docs/PROJECT_PROPOSAL.md) — The original NASA Space Apps proposal.
- [backend/docs/API_CONTRACT.md](backend/docs/API_CONTRACT.md) — The frozen JSON schema for `/analyze` and `/opportunities`.
- [docs/RELEASE_ACCEPTANCE_REPORT.md](docs/RELEASE_ACCEPTANCE_REPORT.md) — Build verification report, including the 5,311 automated test results.

---

## Credits & Tool Disclosures

### AI Tool Usage
In line with NASA Space Apps rules, we used AI tools during development:
- **OpenAI Codex & ChatGPT**: Helped draft coordinate utility functions, unit test cases, and initial API documentation.
- **Google Antigravity & Gemini**: Assisted in repository structure planning, Unity HTTP client debugging, and release test automation.

Every calculation routine, API route, and simulation behavior was reviewed, edited, and tested by team members.

### Acknowledgments
- **NASA Space Apps Challenge 2026** for the *Lunar Landing Site Navigator* challenge.
- **NASA JPL SSD Group** for maintaining the public Horizons API.
- **NASA GSFC / LRO Team** for open LROC and LOLA planetary data.
