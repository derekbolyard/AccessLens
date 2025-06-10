import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardComponent } from '../common/card/card.component';
import { ButtonComponent } from '../common/button/button.component';
import { BadgeComponent } from '../common/badge/badge.component';
import { UpgradeModalComponent } from './upgrade-modal.component';
import { SubscriptionService } from '../../services/subscription.service';
import { SubscriptionPlan, UserSubscription } from '../../types/subscription.interface';
import { Router } from '@angular/router';

@Component({
  selector: 'app-upgrade-page',
  standalone: true,
  imports: [CommonModule, CardComponent, ButtonComponent, BadgeComponent, UpgradeModalComponent],
  templateUrl: './upgrade-page.component.html',
  styleUrls: ['./upgrade-page.component.scss']
})
export class UpgradePageComponent implements OnInit {
  plans: SubscriptionPlan[] = [];
  currentSubscription: UserSubscription | null = null;
  showUpgradeModal = false;
  selectedPlan: SubscriptionPlan | null = null;
  billingInterval: 'month' | 'year' = 'month';

  constructor(
    public subscriptionService: SubscriptionService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.plans = this.subscriptionService.getPlans();
    this.subscriptionService.getCurrentSubscription().subscribe(subscription => {
      this.currentSubscription = subscription;
    });
  }

  get monthlyPlans(): SubscriptionPlan[] {
    return this.plans.filter(plan => plan.interval === 'month');
  }

  get yearlyPlans(): SubscriptionPlan[] {
    return this.plans.filter(plan => plan.interval === 'year');
  }

  get displayedPlans(): SubscriptionPlan[] {
    return this.billingInterval === 'month' ? this.monthlyPlans : this.yearlyPlans;
  }

  setBillingInterval(interval: 'month' | 'year'): void {
    this.billingInterval = interval;
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return this.currentSubscription?.planId === plan.id;
  }

  canUpgrade(plan: SubscriptionPlan): boolean {
    if (!this.currentSubscription) return true;
    
    const currentPlan = this.subscriptionService.getCurrentPlan();
    if (!currentPlan) return true;

    // Can upgrade if the new plan has more scans or is a different interval
    return plan.scanLimit > currentPlan.scanLimit || 
           (plan.scanLimit === currentPlan.scanLimit && plan.interval !== currentPlan.interval);
  }

  onSelectPlan(plan: SubscriptionPlan): void {
    if (this.isCurrentPlan(plan)) return;
    
    this.selectedPlan = plan;
    this.showUpgradeModal = true;
  }

  onCloseUpgradeModal(): void {
    this.showUpgradeModal = false;
    this.selectedPlan = null;
  }

  onUpgradeSuccess(): void {
    this.showUpgradeModal = false;
    this.selectedPlan = null;
    // Refresh subscription data
    this.subscriptionService.getCurrentSubscription().subscribe((subscription: any) => {
      this.currentSubscription = subscription;
    });
  }

  onBackToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  formatPrice(price: number, interval: string): string {
    if (price === 0) return 'Free';
    return `$${price}/${interval}`;
  }

  getYearlySavings(monthlyPrice: number): number {
    const yearlyPrice = monthlyPrice * 10; // 2 months free
    const monthlyCost = monthlyPrice * 12;
    return monthlyCost - yearlyPrice;
  }
}