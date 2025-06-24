export const environment = {
  production: true,
  apiUrl: 'https://your-actual-api-domain.com/api', // ⚠️ UPDATE THIS
  supportEmail: 'support@your-domain.com', // ⚠️ UPDATE THIS
  sentryDsn: '', // ⚠️ ADD YOUR SENTRY DSN HERE
  googleAnalyticsMeasurementId: '', // ⚠️ ADD YOUR GA4 ID HERE
  features: {
    enableAnalytics: true,
    enableErrorReporting: true,
    maxFileUploadSize: 10 * 1024 * 1024,
    scanTimeout: 300000,
    useMagicLinkAuth: true,
    useMockBackend: false, // ✅ Real backend in production
  }
};