# Steam integration

The game is ready for Steam: **signing in**, **achievements**, **Steam Cloud saves**, and a **sign-in ticket**
for a game server, should one ever exist. Everything runs offline until Steamworks.NET is installed and an App
ID is set. The game then switches to Steam by itself.

Store-page assets (capsules, screenshots, trailer, store text) are separate, in [`Marketing/Steam/`](../Marketing/Steam).

## What the player gets

- **Signing in.** Steam games don't have a login screen: the game runs as whoever is signed in to Steam. Their
  Steam name appears in Doug's first line ("Hey Sam, I'm Doug."), in the welcome back, on the founder's card and
  in the menu ("Signed in to Steam as Sam"). If Steam isn't running, the game plays offline and the menu says why.
- **Achievements.** There are 24 (see [achievements.md](achievements.md)). They unlock on Steam with Steam's pop-up
  and the game's own toast. The menu lists them all with progress ("Cat Person 4/10"), and hidden ones stay
  secret. Offline, they're remembered on the computer and sent to Steam the next time it's available.
- **Steam Cloud.** The save follows the player to every computer:
  - The game writes `studio.isav` to Steam Cloud each time it saves, and checks the cloud copy when it starts.
  - Whichever copy changed since the two last matched wins.
  - If both changed (offline play here and online play elsewhere), the player picks the studio to keep, from
    cards showing each one's name, day, money and save time.
  - The other copy is kept beside the save, never deleted.
- **Sign-in ticket for a server.** `Platform.RequestAuthTicket(audience, ticket => …)` returns a Steam Web API
  ticket (hex). A server checks it with the `ISteamUserAuth/AuthenticateUserTicket` Web API, using the same
  `audience` string and your publisher key. Nothing uses it yet; it's there for online features (cloud
  leaderboards, a community site).

## How it fits together

| Path | What it is |
|---|---|
| `Assets/IndieStudio/Simulation/Content/Achievements.cs` | The 24 achievements and their rules (plain C#, tested in `AchievementTests`) |
| `Assets/IndieStudio/Simulation/Core/SaveSync.cs` | Which save copy wins: this computer's, the cloud's, or the player's choice |
| `Assets/IndieStudio/Game/Platform/` | `IPlatform` (Steam or offline), `OfflinePlatform`, `AchievementTracker`, and `FolderCloudStorage` (a folder that stands in for the cloud in tests) |
| `Assets/IndieStudio/Game/Persistence.cs` | The save on disk and its cloud copy, including the conflict handling |
| `Assets/IndieStudio/Steam/` | **The Steam module:** `SteamPlatform` (Steamworks.NET) and `SteamSettings` (your App ID). Its assembly only compiles once Steamworks.NET is installed: a version define sets `STEAMWORKS_NET` when the package is present. Without the package, the game builds and plays offline. |
| `SimHarness/SteamCompileCheck/` | Type-checks the Steam module without Unity, against a stand-in for the Steamworks.NET API (`dotnet build SimHarness/SteamCompileCheck`) |

## Turning it on

1. **Get an App ID.** In the Steamworks partner site, pay the Steam Direct fee and create the app. Until then,
   **480** (Valve's "Spacewar" test app) works for trying sign-in and Steam Cloud.
2. **Install Steamworks.NET.** In Unity, open **Window → Package Manager**, then **+ → Add package from git URL…**:
   `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1`
   This needs git installed. A newer release tag is fine; the Steam module avoids the parts of the API that
   changed recently.
3. **Set the App ID** in `Assets/IndieStudio/Steam/SteamSettings.cs`.
4. **For the editor:** create `steam_appid.txt` in the project folder (next to `Assets/`), containing only the
   App ID. Never ship it with a release build.
5. **Set up the app in Steamworks:**
   - **Stats & Achievements:** enter the 24 achievements from [achievements.md](achievements.md) with their
     icons, then publish.
   - **Steam Cloud:**
     - Turn it on with **Byte quota per user** 4,000,000 and **Number of files allowed per user** 10.
       A two-year save is about 70 KB.
     - Leave the Auto-Cloud paths empty: the game syncs the save itself.
     - Publish.
6. **Release builds:** set `RestartThroughSteam` to `true` in `SteamSettings.cs`, so a copy started outside Steam
   asks Steam to start it (that's what gives it the overlay and the signed-in player).

Then start Steam, sign in, and press Play. The menu should say "Signed in to Steam as …".

## Testing

- **Automated** (no Steam needed):
  - `PlatformTests` play the game with a fake Steam signed in as "Tester". The studio follows them across two
    "computers" that share a folder as the cloud, a real conflict asks which studio to keep, achievements unlock
    with a toast (and quietly when earned before), and the menu shows who's signed in.
  - `AchievementTests` (also run by `dotnet test SimHarness/Tests`) check the rules, the cloud decision, and that
    this sheet matches the game.
- **With the Spacewar App ID (480):** sign-in and Steam Cloud work, but Steam refuses this game's achievements.
  The game retries for a couple of minutes, then logs a warning.
- **Clearing achievements:** open the Steam console (`steam://open/console` in a browser) and use
  `achievement_clear <appid> <API name>` or `reset_all_stats <appid>`.
- **Cloud conflicts by hand:**
  1. Play on two computers.
  2. Go offline on one of them (or turn Steam Cloud off for the game in its Steam properties) and play on.
  3. Go back online.
