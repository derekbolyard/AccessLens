export class Validators {
  static isValidUrl(url: string): boolean {
    if (!url || typeof url !== 'string') return false;
    
    try {
      const urlObj = new URL(url);
      return urlObj.protocol === 'http:' || urlObj.protocol === 'https:';
    } catch {
      return false;
    }
  }

  static isValidEmail(email: string): boolean {
    if (!email || typeof email !== 'string') return false;
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  static sanitizeInput(input: string): string {
    if (!input || typeof input !== 'string') return '';
    
    return input.trim().replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  static isValidDomain(domain: string): boolean {
    if (!domain || typeof domain !== 'string') return false;
    
    const domainRegex = /^[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9](?:\.[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9])*$/;
    return domainRegex.test(domain);
  }
}