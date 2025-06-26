export const environment = {
  production: false,
  apiUrl: '/api', // Using the proxy configuration to forward to https://localhost:7088
  supportEmail: 'support@accessibilityreports.com',
  sentryDsn: '', // Add your Sentry DSN here for development (optional)
  googleAnalyticsMeasurementId: '', // Add your GA4 Measurement ID here for development (optional)
  features: {
    enableAnalytics: false,
    enableErrorReporting: false,
    maxFileUploadSize: 10 * 1024 * 1024,
    scanTimeout: 300000,
    useMagicLinkAuth: true,
    useMockBackend: false
  }
};