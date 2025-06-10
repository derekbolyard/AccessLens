import { Injectable } from '@angular/core';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { SubscriptionPlan, UserSubscription, UpgradeRequest, User } from '../types/subscription.interface';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
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
      id: 'starter-yearly',
      name: 'Starter',
      stripeProductId: 'price_starter_yearly',
      price: 290,
      interval: 'year',
      scanLimit: 120,
      features: ['120 scans per year', 'Basic reporting', 'Email support'],
      isPopular: true,
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
    }
  ];

  private currentSubscriptionSubject = new BehaviorSubject<UserSubscription | null>(null);
  private currentUserSubject = new BehaviorSubject<User | null>(null);

  constructor() {
    // Mock current subscription
    this.currentSubscriptionSubject.next({
      id: 'sub_123',
      planId: 'starter-monthly',
      plan: this.plans[0],
      status: 'active',
      startDate: new Date(),
      endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
      email: 'user@example.com',
      active: true
    });

    // Mock current user
    this.currentUserSubject.next({
      userId: 'user_123',
      email: 'user@example.com',
      emailVerified: true,
      firstScan: false,
      createdAt: new Date(),
      scansUsed: 3,
      scanLimit: 10,
      scansRemaining: 7
    });
  }

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
    return this.upgrade(request);
  }

  getCurrentUser(): Observable<User | null> {
    return this.currentUserSubject.asObservable();
  }

  getCurrentPlan(): SubscriptionPlan | null {
    const subscription = this.currentSubscriptionSubject.value;
    const plan = subscription?.planId ? this.getPlanById(subscription.planId) : undefined;
    return plan || null;
  }

  private getPlanById(id: string): SubscriptionPlan | undefined {
    return this.plans.find(plan => plan.id === id);
  }

  private upgrade(request: UpgradeRequest): Observable<UserSubscription> {
    const newPlan = this.getPlanById(request.planId);
    if (newPlan) {
      const newSubscription: UserSubscription = {
        id: 'sub_' + Math.random().toString(36).substr(2, 9),
        planId: newPlan.id,
        plan: newPlan,
        status: 'active',
        startDate: new Date(),
        endDate: new Date(Date.now() + (newPlan.interval === 'year' ? 365 : 30) * 24 * 60 * 60 * 1000),
        email: 'user@example.com',
        active: true
      };
      this.currentSubscriptionSubject.next(newSubscription);
      return of(newSubscription);
    }
    throw new Error('Plan not found');
  }
}