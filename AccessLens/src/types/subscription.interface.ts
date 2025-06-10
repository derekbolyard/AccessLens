export interface SubscriptionPlan {
  id: string;
  name: string;
  price: number;
  interval: 'month' | 'year';
  features: string[];
  scanLimit: number;
  popular?: boolean;
}

export interface UserSubscription {
  planId: string;
  status: 'active' | 'canceled' | 'past_due' | 'trialing';
  currentPeriodStart: Date;
  currentPeriodEnd: Date;
  scansUsed: number;
  scanLimit: number;
}

export interface UpgradeRequest {
  planId: string;
  interval: 'month' | 'year';
}