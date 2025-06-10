import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { SubscriptionPlan, UserSubscription, UpgradeRequest } from '../types/subscription.interface';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private subscriptionSubject = new BehaviorSubject<UserSubscription | null>(null);
  public subscription$ = this.subscriptionSubject.asObservable();

  private plans: SubscriptionPlan[] = [
    {
      id: 'free',
      name: 'Free',
      price: 0,
      interval: 'month',
      scanLimit: 3,
      features: [
        '3 scans per month',
        'Basic accessibility reports',
        'Issue tracking',
        'Email support'
      ]
    },
    {
      id: 'pro',
      name: 'Professional',
      price: 29,
      interval: 'month',
      scanLimit: 50,
      popular: true,
      features: [
        '50 scans per month',
        'Advanced accessibility reports',
        'Priority issue tracking',
        'Custom reporting',
        'API access',
        'Priority support'
      ]
    },
    {
      id: 'pro-yearly',
      name: 'Professional',
      price: 290,
      interval: 'year',
      scanLimit: 50,
      features: [
        '50 scans per month',
        'Advanced accessibility reports',
        'Priority issue tracking',
        'Custom reporting',
        'API access',
        'Priority support',
        '2 months free'
      ]
    },
    {
      id: 'enterprise',
      name: 'Enterprise',
      price: 99,
      interval: 'month',
      scanLimit: 200,
      features: [
        '200 scans per month',
        'Enterprise accessibility reports',
        'Advanced analytics',
        'White-label reports',
        'SSO integration',
        'Dedicated support',
        'Custom integrations'
      ]
    },
    {
      id: 'enterprise-yearly',
      name: 'Enterprise',
      price: 990,
      interval: 'year',
      scanLimit: 200,
      features: [
        '200 scans per month',
        'Enterprise accessibility reports',
        'Advanced analytics',
        'White-label reports',
        'SSO integration',
        'Dedicated support',
        'Custom integrations',
        '2 months free'
      ]
    }
  ];

  constructor() {
    this.loadMockSubscription();
  }

  private loadMockSubscription(): void {
    // Mock free tier subscription
    const mockSubscription: UserSubscription = {
      planId: 'free',
      status: 'active',
      currentPeriodStart: new Date(),
      currentPeriodEnd: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000), // 30 days from now
      scansUsed: 2, // User has used 2 out of 3 free scans
      scanLimit: 3
    };

    this.subscriptionSubject.next(mockSubscription);
  }

  getPlans(): SubscriptionPlan[] {
    return this.plans;
  }

  getCurrentSubscription(): Observable<UserSubscription | null> {
    return this.subscription$;
  }

  canRequestScan(): boolean {
    const subscription = this.subscriptionSubject.value;
    if (!subscription) return false;
    return subscription.scansUsed < subscription.scanLimit;
  }

  getScansRemaining(): number {
    const subscription = this.subscriptionSubject.value;
    if (!subscription) return 0;
    return Math.max(0, subscription.scanLimit - subscription.scansUsed);
  }

  incrementScanUsage(): void {
    const subscription = this.subscriptionSubject.value;
    if (subscription && subscription.scansUsed < subscription.scanLimit) {
      const updatedSubscription = {
        ...subscription,
        scansUsed: subscription.scansUsed + 1
      };
      this.subscriptionSubject.next(updatedSubscription);
    }
  }

  upgradePlan(request: UpgradeRequest): Observable<boolean> {
    // Simulate upgrade process
    return of(true).pipe(
      delay(2000),
      map(success => {
        if (Math.random() > 0.9) { // 10% failure rate for demo
          throw new Error('Payment processing failed');
        }

        // Update subscription
        const plan = this.plans.find(p => p.id === request.planId);
        if (plan) {
          const updatedSubscription: UserSubscription = {
            planId: plan.id,
            status: 'active',
            currentPeriodStart: new Date(),
            currentPeriodEnd: new Date(Date.now() + (plan.interval === 'year' ? 365 : 30) * 24 * 60 * 60 * 1000),
            scansUsed: 0,
            scanLimit: plan.scanLimit
          };
          this.subscriptionSubject.next(updatedSubscription);
        }

        return success;
      })
    );
  }

  getPlanById(planId: string): SubscriptionPlan | undefined {
    return this.plans.find(plan => plan.id === planId);
  }

  getCurrentPlan(): SubscriptionPlan | undefined {
    const subscription = this.subscriptionSubject.value;
    if (!subscription) return undefined;
    return this.getPlanById(subscription.planId);
  }
}