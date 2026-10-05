# Deploy Elementary to Cloudflare Workers

This configuration publishes all three translations as static assets. No Worker JavaScript, R2 bucket, or server-side .NET runtime is needed. It explicitly disables AOT for this deployment because the AOT runtime exceeds Cloudflare's 25 MiB per-asset limit.

## Prerequisites

- A Cloudflare account.
- Node.js with npm/npx installed.
- The .NET 10 SDK and Uno WebAssembly workload already used to build this project locally.

## Deploy from PowerShell

Open PowerShell in the repository root:

```powershell
cd C:\Users\EzraW\Documents\GitHub\Elementary
npx wrangler@4 login
npx wrangler@4 deploy
```

Login opens a browser for Cloudflare authorization. Deploy reads `wrangler.jsonc`, runs the non-AOT Release publish, and uploads the generated `bin/Cloudflare/wwwroot` assets. It prints the public workers.dev URL. The configured Worker name is `elementary-bible`; change `name` before deployment if you want a different name. Subsequent deploys update that same Worker.

Repeat `npx wrangler@4 deploy` after app changes. You do not need to log in again while your authorization remains valid. Build and upload may take several minutes.

## Review before publishing

```powershell
npx wrangler@4 deploy --dry-run
```

This prepares the deployment without publishing. To review the generated files locally after publishing/building, serve `src/Elementary.Uno/Elementary.Uno/bin/Cloudflare/wwwroot` with a static web server.

The existing AOT Release setting is retained for other deployment targets; the Cloudflare command overrides it. Do not upload the `aot-preview` folder to Workers Static Assets.

## After publishing

Open the URL printed by Wrangler. Check reading, translation selection, search, and verse of the day. Browser-local settings/history are separate for the new domain. A custom domain can be added from the Worker's Settings → Domains & Routes.

## References

- https://developers.cloudflare.com/workers/static-assets/get-started/
- https://developers.cloudflare.com/workers/wrangler/commands/workers/
- https://developers.cloudflare.com/workers/platform/limits/

Cloudflare deployment has not been performed by the assistant; these commands publish under your account when you run them.
