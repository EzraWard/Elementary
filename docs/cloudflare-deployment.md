# Deploy Elementary to Cloudflare Workers

This configuration publishes all three translations as static assets. No Worker JavaScript, R2 bucket, or server-side .NET runtime is needed. It explicitly disables AOT for this deployment because the AOT runtime exceeds Cloudflare's 25 MiB per-asset limit.

## Prerequisites

- A Cloudflare account.
- Node.js with npm/npx installed.
- The .NET 11 SDK and WebAssembly workload already used to build this project locally.

## Deploy from PowerShell

Open PowerShell in the repository root:

```powershell
cd C:\Users\EzraW\Documents\GitHub\Elementary
npx wrangler@4 login
npx wrangler@4 deploy --env=
```

Login opens a browser for Cloudflare authorization. Deploy reads `wrangler.jsonc`, runs the non-AOT Release publish, and uploads the generated `bin/Cloudflare/wwwroot` assets. It prints the public workers.dev URL. The configured Worker name is `elementary-bible`; change `name` before deployment if you want a different name. Subsequent deploys update that same Worker.

Repeat `npx wrangler@4 deploy --env=` after app changes. You do not need to log in again while your authorization remains valid. Build and upload may take several minutes.

## Deploy the preview

```powershell
npx wrangler@4 deploy --env preview
```

This publishes **Elementary Web Preview** to the separate `elementary-bible-preview` Worker. Install it from that Worker's stable URL and install **Elementary Web** from the production Worker's URL. Each origin has its own install and browser-local settings and history. Use the stable Worker URLs for installs you want to keep across deployments.

## Review before publishing

```powershell
npx wrangler@4 deploy --env= --dry-run
npx wrangler@4 deploy --env preview --dry-run
```

These commands prepare the deployments without publishing. To review the generated files locally, serve `src/Elementary.Uno/Elementary.Uno/bin/Cloudflare/wwwroot` for production and `src/Elementary.Uno/Elementary.Uno/bin/CloudflarePreview/wwwroot` for preview on separate ports.

The existing AOT Release setting is retained for other deployment targets; the Cloudflare command overrides it. Do not upload the `aot-preview` folder to Workers Static Assets.

## After publishing

Open the URL printed by Wrangler. Check reading, translation selection, search, and verse of the day. Browser-local settings/history are separate for the new domain. A custom domain can be added from the Worker's Settings → Domains & Routes.

## References

- https://developers.cloudflare.com/workers/static-assets/get-started/
- https://developers.cloudflare.com/workers/wrangler/commands/workers/
- https://developers.cloudflare.com/workers/platform/limits/

Cloudflare deployment has not been performed by the assistant; these commands publish under your account when you run them.
