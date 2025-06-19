export interface BrandingInfo {
  id: string;
  userId: string;
  logoUrl: string;
  primaryColor: string;
  secondaryColor: string;
  companyName?: string;
  contactEmail?: string;
  website?: string;
  reportFooterText?: string;
}

export const DEFAULT_BRANDING: BrandingInfo = {
  id: '',
  userId: '',
  logoUrl: '',
  primaryColor: '#4f46e5',
  secondaryColor: '#e0e7ff',
  companyName: 'My Company',
  contactEmail: '',
  website: '',
  reportFooterText: 'Powered by AccessLens'
};