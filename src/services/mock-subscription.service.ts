import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { SubscriptionPlan, UserSubscription, UpgradeRequest, User } from '../types/subscription.interface';

@Injectable({
  providedIn: 'root'
})
export class MockSubscriptionService {
  private plans: SubscriptionPlan[] = [
    {
      id: 'starter-monthly',
      name: 'Starter',
      stripeProductId: 'price_starter_monthly',
      price: 29,
      interval: 'month',
      scanLimit: 10,
      features: ['10 scans per month', 'Basic reporting', 'Email support'],
      isPopular: false,
      isActive: true
    },
    {
      id: 'professional-monthly',
      name: 'Professional',
      stripeProductId: 'price_pro_monthly',
      price: 99,
      interval: 'month',
      scanLimit: 50,
      features: ['50 scans per month', 'Advanced reporting', 'Priority support', 'API access'],
      isPopular: true,
      isActive: true
    },
    {
      id: 'enterprise-monthly',
      name: 'Enterprise',
      stripeProductId: 'price_enterprise_monthly',
      price: 299,
      interval: 'month',
      scanLimit: 200,
      features: ['200 scans per month', 'Premium reporting', 'Phone support', 'API access', 'Custom integrations', 'SSO'],
      isPopular: false,
      isActive: true
    },
    // Yearly variants
    {
      id: 'starter-yearly',
      name: 'Starter',
      stripeProductId: 'price_starter_yearly',
      price: 290,
      interval: 'year',
      scanLimit: 120,
      features: ['120 scans per year', 'Basic reporting', 'Email support'],
      isPopular: false,
      isActive: true
    },
    {
      id: 'professional-yearly',
      name: 'Professional',
      stripeProductId: 'price_pro_yearly',
      price: 990,
      interval: 'year',
      scanLimit: 600,
      features: ['600 scans per year', 'Advanced reporting', 'Priority support', 'API access'],
      isPopular: false,
      isActive: true
    },
    {
      id: 'enterprise-yearly',
      name: 'Enterprise',
      stripeProductId: 'price_enterprise_yearly',
      price: 2990,
      interval: 'year',
      scanLimit: 2400,
      features: ['2400 scans per year', 'Premium reporting', 'Phone support', 'API access', 'Custom integrations', 'SSO'],
      isPopular: false,
      isActive: true
    }
  ];

  private currentSubscriptionSubject = new BehaviorSubject<UserSubscription | null>({
    id: 'sub_123',
    planId: 'starter-monthly',
    plan: this.plans[0],
    status: 'active',
    startDate: new Date(),
    endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
    email: 'user@example.com',
    active: true
  });

  private currentUserSubject = new BehaviorSubject<User | null>({
    userId: 'user_123',
    email: 'user@example.com',
    emailVerified: true,
    firstScan: false,
    createdAt: new Date(),
    scansUsed: 3,
    scanLimit: 10,
    scansRemaining: 7
  });

  getPlans(): SubscriptionPlan[] {
    return this.plans;
  }

  getCurrentSubscription(): Observable<UserSubscription | null> {
    return this.currentSubscriptionSubject.asObservable();
  }

  canRequestScan(): boolean {
    const user = this.currentUserSubject.value;
    return user ? user.scansUsed < user.scanLimit : false;
  }

  getScansRemaining(): number {
    const user = this.currentUserSubject.value;
    return user ? Math.max(0, user.scanLimit - user.scansUsed) : 0;
  }

  incrementScanUsage(): Observable<User | null> {
    const user = this.currentUserSubject.value;
    if (user && user.scansUsed < user.scanLimit) {
      const updatedUser = {
        ...user,
        scansUsed: user.scansUsed + 1,
        scansRemaining: user.scanLimit - (user.scansUsed + 1)
      };
      this.currentUserSubject.next(updatedUser);
      return of(updatedUser);
    }
    return of(user);
  }

  upgradePlan(request: UpgradeRequest): Observable<UserSubscription> {
    const newPlan = this.plans.find(p => p.id === request.planId);
    if (newPlan) {
      const newSubscription: UserSubscription = {
        id: 'sub_' + Math.random().toString(36).substring(2, 11),
        planId: newPlan.id,
        plan: newPlan,
        status: 'active',
        startDate: new Date(),
        endDate: new Date(Date.now() + (newPlan.interval === 'year' ? 365 : 30) * 24 * 60 * 60 * 1000),
        email: 'user@example.com',
        active: true
      };
      
      this.currentSubscriptionSubject.next(newSubscription);
      
      // Update user scan limit based on new plan
      const currentUser = this.currentUserSubject.value;
      if (currentUser) {
        this.currentUserSubject.next({
          ...currentUser,
          scanLimit: newPlan.scanLimit,
          scansRemaining: newPlan.scanLimit - currentUser.scansUsed
        });
      }
      
      return of(newSubscription);
    }
    throw new Error('Plan not found');
  }

  getCurrentUser(): Observable<User | null> {
    return this.currentUserSubject.asObservable();
  }

  getCurrentPlan(): SubscriptionPlan | null {
    const subscription = this.currentSubscriptionSubject.value;
    return subscription?.plan || null;
  }
}