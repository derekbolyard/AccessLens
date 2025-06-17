# AccessLens

This repository contains the AccessLens API and front‑end. The API can be configured via environment variables.

## Required environment variables

The API reads configuration values from `appsettings.json` and the following environment variables which override those defaults:

- `SQLITE_CONNECTION_STRING` – SQLite connection string (defaults to `Data Source=accesslens.db`)
- `GMAIL_FROM_EMAIL` – address used to send email
- `GMAIL_OAUTH_CLIENT_ID` – Gmail OAuth client ID
- `GMAIL_OAUTH_CLIENT_SECRET` – Gmail OAuth client secret
- `GMAIL_REFRESH_TOKEN` – long‑lived Gmail refresh token
- `STRIPE_SECRET_KEY` – Stripe secret API key
- `STRIPE_WEBHOOK_SECRET` – Stripe webhook signing secret

Set these variables before launching the API for production or development.

## Deploying to Fly.io

1. Install the [Fly.io CLI](https://fly.io/docs/hands-on/install-flyctl/).
2. Authenticate with `fly auth login`.
3. Create an app with `fly launch` or use the provided `fly.toml`.
4. Run `fly deploy` to build and deploy the container.

The provided `Dockerfile` builds both the ASP.NET API and Angular front‑end and exposes the application on port `8080`.

When deployed, the API serves the compiled Angular application from the `wwwroot` folder so the front‑end is accessible on the same domain.
`wwwroot` is the default location for static files in ASP.NET, so no additional configuration is required.
