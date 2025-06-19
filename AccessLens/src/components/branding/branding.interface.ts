export interface BrandingSettings {
  id: string;
  userId: string;
  companyName: string;
  logo?: string; // URL or base64
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  fontFamily?: string;
  reportFooterText?: string;
  contactEmail?: string;
  website?: string;
  createdDate: Date;
  updatedDate: Date;
}

export interface BrandingPreview {
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  logo?: string;
  companyName: string;
}

export const DEFAULT_BRANDING: Partial<BrandingSettings> = {
  companyName: 'Your Company',
  primaryColor: '#2563eb',
  secondaryColor: '#64748b',
  accentColor: '#10b981',
  fontFamily: 'Inter',
  reportFooterText: 'Powered by AccessibilityReports'
};