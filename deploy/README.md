# Deploying the Production Audit System

Publishing straight over the running IIS site fails with:

> Could not copy `TL.dll` ... the process cannot access the file because it is
> being used by another process. Exceeded retry count of 10. Failed.

That's the live app holding a lock on its own DLL. The fix is to stop the app
(drop `app_offline.htm`), copy the new build, then start it again. **`deploy.ps1`
does all of that automatically and safely.**

## One-click deploy

From the repo root, in PowerShell:

```powershell
./deploy/deploy.ps1
```

That will:

1. Publish a fresh **Release** build to a local staging folder (a build error
   here stops everything — the live site is never touched).
2. Copy `app_offline.htm` to the site → IIS stops the app, releasing the lock.
3. Copy the new build onto the site.
4. Remove `app_offline.htm` → IIS restarts on the new build.

`app_offline.htm` is **always** removed at the end, even if a step fails, so the
site can't get stuck in maintenance mode. If it ever can't remove it (network
blip), the script prints the exact path to delete manually.

### Different server path

```powershell
./deploy/deploy.ps1 -Site "\\csm-srv-16\c$\inetpub\wwwroot\TL portal"
```

## One-time server setup (Data Protection keys)

The app now persists its Data Protection keys so restarts/redeploys no longer
invalidate antiforgery tokens (which was silently breaking audit saves). By
default keys go to a `TL-dataprotection-keys` folder **next to** the site
folder. Make sure the IIS app-pool identity can write there. To use a different
location, add to `appsettings.json`:

```json
"DataProtection": { "KeyPath": "D:\\TL-keys" }
```

## Manual fallback

If you can't run the script, do it by hand:

1. Copy `deploy/app_offline.template.htm` to the site as `app_offline.htm`.
2. Publish from Visual Studio (the copy now succeeds).
3. Delete `app_offline.htm` from the site.
