# AccessLens

This repository contains the AccessLens API and front‑end. The API can be configured via environment variables.

## Required environment variables

The API reads configuration values from environment variables. These may be loaded from a `.env` file for local development. Environment variables always override settings from `appsettings.json`.

Set the following variables before running the service:

- `SQLITE_CONNECTION_STRING` – SQLite connection string
- `MAGIC_JWT_SECRET` – secret key for JWT signing
- `AWS_REGION` – AWS region for S3/SES
- `AWS_S3_BUCKET` – S3 bucket for uploaded reports
- `AWS_SES_FROM_EMAIL` – from address when using SES
- `GMAIL_FROM_EMAIL` – address used to send email
- `GMAIL_OAUTH_CLIENT_ID` – Gmail OAuth client ID
- `GMAIL_OAUTH_CLIENT_SECRET` – Gmail OAuth client secret
- `GMAIL_REFRESH_TOKEN` – long‑lived Gmail refresh token
- `SENDGRID_API_KEY` – SendGrid API key
- `SENDGRID_FROM_EMAIL` – address used to send email via SendGrid
- `STRIPE_SECRET_KEY` – Stripe secret API key
- `STRIPE_WEBHOOK_SECRET` – Stripe webhook signing secret
- `FRONTEND_BASE_URL` – base URL of the Angular app
- `GCS_BUCKET_NAME` – Google Cloud Storage bucket (optional)
- `GCS_SERVICE_ACCOUNT_JSON` – service account JSON for GCS (optional)
- `STORAGE_PROVIDER` – `s3` (default), `gcs`, or `local`
- `LOCAL_STORAGE_ROOT` – directory for files when using local storage (optional)
- `BASE_URL` – public URL root for local storage (optional)
- `AWS_SERVICE_URL` – custom S3-compatible endpoint (use `http://localhost:9000` for MinIO)
- `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` – credentials for the bundled MinIO server

Set these variables before launching the API for production or development.

## Deploying to Fly.io

1. Install the [Fly.io CLI](https://fly.io/docs/hands-on/install-flyctl/).
2. Authenticate with `fly auth login`.
3. Create an app with `fly launch` or use the provided `fly.toml`.
4. Run `fly deploy` to build and deploy the container.

The provided `Dockerfile` builds both the ASP.NET API and Angular front‑end and exposes the application on port `8080`.

The `fly.toml` config defines a separate `minio` process that runs a MinIO server on port `9000`. Attach a Fly volume to `/data` for persistence and set `AWS_SERVICE_URL=http://localhost:9000` so the API stores files in MinIO.

When deployed, the API serves the compiled Angular application from the `wwwroot` folder so the front‑end is accessible on the same domain.
`wwwroot` is the default location for static files in ASP.NET, so no additional configuration is required.
