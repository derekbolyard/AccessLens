# AccessLens API

This directory contains the ASP.NET Core backend that powers the AccessLens service.

## Required environment variables

Configuration settings are read from environment variables, which take precedence over `appsettings.json`. You can create a `.env` file for local development using the `Section__Key` notation. Define the following variables:

- `AWS__Region` – AWS region used when uploading files (or `AWS_REGION`).
- `AWS__S3Bucket` – S3 bucket name for PDF and image uploads (or `AWS_S3_BUCKET`).
- `AWS_SES_FROM_EMAIL` – sender address when using Amazon SES.
- `Stripe__SecretKey` – your Stripe secret API key.
- `Stripe__WebhookSecret` – signing secret for webhook verification.
- `Gmail__FromEmail` – address the service sends email from.
- `Gmail__OAuthClientId` – Gmail OAuth client ID.
- `Gmail__OAuthClientSecret` – Gmail OAuth client secret.
- `Gmail__RefreshToken` – long‑lived refresh token for Gmail API access.
- `Captcha__hCaptchaSecret` – secret key for hCaptcha verification.
- `Gcs__BucketName` – (if using Google Cloud Storage) bucket name.
- `Gcs__ServiceAccountJson` or `GCS_SERVICE_ACCOUNT_JSON` – JSON for the service account used when storing files in GCS.
- `ConnectionStrings__SqliteConnection` – connection string for the SQLite database.
- `Playwright__BrowsersPath` – optional custom path for Playwright browsers.
- `MAGIC_JWT_SECRET` – secret key for JWT magic link signing (minimum 32 characters).
 - `Frontend__BaseUrl` – base URL of the frontend application for magic link redirects (or `FRONTEND_BASE_URL`).

## Database migrations

Entity Framework Core migrations are included in the `Migrations` folder. To apply them locally run:

```bash
dotnet ef database update
```

This creates or updates the `accesslens.db` SQLite database.

## Running the API locally

From this directory run:

```bash
dotnet run
```

The server listens on the URLs configured in `launchSettings.json` (by default `https://localhost:7088`).

## Available API endpoints

The main endpoints exposed by the API are listed below.

### Authentication

- `POST /api/auth/send-magic-link` – send a JWT magic link to a user's email.
- `GET /api/auth/magic/{token}` – verify the magic link JWT and redirect to frontend with session token.

### Scanning

- `POST /api/scan/starter` – perform a five‑page accessibility snapshot.
- `POST /api/scan/full` – crawl and scan an entire site (requires premium access).

### Stripe webhooks

- `POST /stripe/webhook` – handle Stripe billing events.

Additional utility endpoints exist for storage testing and the example weather forecast controller used by the ASP.NET template.

## Authentication Flow

AccessLens uses a secure JWT-based magic link authentication system:

1. User requests a magic link via `POST /api/auth/send-magic-link`
2. System generates a short-lived JWT (15 minutes) and emails it as a clickable link
3. User clicks the link, which calls `GET /api/auth/magic/{token}`
4. System validates the JWT, marks email as verified, and redirects with a session token
5. Frontend receives the session token for API authentication

This eliminates the need for passwords or manually entered codes while maintaining security.