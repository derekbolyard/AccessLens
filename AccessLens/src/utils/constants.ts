export const APP_CONSTANTS = {
  STORAGE_KEYS: {
    USER: 'accessibility_reports_user',
    THEME: 'accessibility_reports_theme',
    PREFERENCES: 'accessibility_reports_preferences'
  },
  
  API_ENDPOINTS: {
    SITES: '/sites',
    REPORTS: '/reports',
    SCANS: '/scans',
    AUTH: '/auth',
    SUBSCRIPTION: '/subscription'
  },

  SCAN_LIMITS: {
    FREE: 3,
    PRO: 50,
    ENTERPRISE: 200
  },

  ISSUE_TYPES: {
    ERROR: 'error',
    WARNING: 'warning',
    NOTICE: 'notice'
  } as const,

  ISSUE_IMPACTS: {
    CRITICAL: 'critical',
    SERIOUS: 'serious',
    MODERATE: 'moderate',
    MINOR: 'minor'
  } as const,

  WCAG_LEVELS: {
    A: 'A',
    AA: 'AA',
    AAA: 'AAA'
  } as const
};

export type IssueType = typeof APP_CONSTANTS.ISSUE_TYPES[keyof typeof APP_CONSTANTS.ISSUE_TYPES];
export type IssueImpact = typeof APP_CONSTANTS.ISSUE_IMPACTS[keyof typeof APP_CONSTANTS.ISSUE_IMPACTS];
export type WcagLevel = typeof APP_CONSTANTS.WCAG_LEVELS[keyof typeof APP_CONSTANTS.WCAG_LEVELS];