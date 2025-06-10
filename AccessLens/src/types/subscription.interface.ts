export interface SubscriptionPlan {
  id: string;
  name: string;
  stripeProductId: string;
  price: number;
  interval: 'month' | 'year';
  scanLimit: number;
  features: string[];
  isPopular: boolean;
  isActive: boolean;
  description?: string;
}

export interface UserSubscription {
  id: string;
  planId?: string;
  plan?: SubscriptionPlan;
  status: string;
  startDate?: Date;
  endDate?: Date;
  email: string;
  active: boolean;
}

export interface User {
  userId: string;
  email: string;
  emailVerified: boolean;
  firstScan: boolean;
  createdAt: Date;
  scansUsed: number;
  scanLimit: number;
  scansRemaining: number; // calculated property
}

export interface UpgradeRequest {
  planId: string;
  interval: 'month' | 'year';
}