# HSL City Bikes

A Blazor WebAssembly app showing real-time Helsinki city bike station availability, trend tracking, cycle lane overlay, and historical trip data.

**Live:** https://kuoste.github.io/hsl-bike-app/

## Features

- Real-time bike station availability from Digitransit GBFS API
- Interactive Leaflet.js map with color-coded station markers
- Availability trend tracking (instant on page load via pre-built snapshots)
- Change indicators showing bikes rented/returned between refreshes
- Station detail panel with popular trip destinations
- Helsinki cycle lane overlay from open WFS data
- Dark mode support (follows OS preference)
- Auto-refresh every 30 seconds + manual refresh

## Architecture

- **Blazor WebAssembly** — standalone, hosted as static files on GitHub Pages
- **Leaflet.js** — raw JS interop for map rendering (OSM tiles)
- **Digitransit GBFS API** — real-time station data
- **GitHub Actions** — snapshot poller (every 5 min) for instant trend data on load
- **hsl-bike-data-aggregator** — historical trip data (separate repo)

## Setup

1. Clone the repo
2. Set your Digitransit subscription key in `src/HslBikeApp/wwwroot/appsettings.json`:
   ```json
   { "DigitransitSubscriptionKey": "your-key-here" }
   ```
3. Run locally:
   ```bash
   dotnet run --project src/HslBikeApp
   ```
4. Run tests:
   ```bash
   dotnet test
   ```

## API Key

Get a free API key from the [Digitransit API portal](https://portal-api.digitransit.fi/).  
The key is used for rate-limiting — it's a public transit API, not a secret.

For GitHub Actions snapshot poller, add `DIGITRANSIT_SUBSCRIPTION_KEY` as a repository secret.

## Project Structure

```
src/HslBikeApp/          — Blazor WASM app
tools/FetchSnapshot/      — Console app for GH Actions snapshot poller
tests/HslBikeApp.Tests/   — xUnit + bUnit tests
.github/workflows/        — CI/CD + snapshot poller
```
