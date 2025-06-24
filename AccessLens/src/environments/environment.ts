export const environment = {
  production: false,
  apiUrl: 'http://localhost:3000/api',
  supportEmail: 'support@accessibilityreports.com',
  sentryDsn: '', // Add your Sentry DSN here for development (optional)
  googleAnalyticsMeasurementId: '', // Add your GA4 Measurement ID here for development (optional)
  features: {
    enableAnalytics: false,
    enableErrorReporting: false,
    maxFileUploadSize: 10 * 1024 * 1024,
    scanTimeout: 300000,
    useMagicLinkAuth: true,
    useMockBackend: true, // Enable mock backend in development
  }
};