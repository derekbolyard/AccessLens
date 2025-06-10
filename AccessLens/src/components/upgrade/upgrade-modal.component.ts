import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../common/modal/modal.component';
import { ButtonComponent } from '../common/button/button.component';
import { AlertComponent } from '../common/alert/alert.component';
import { SubscriptionService } from '../../services/subscription.service';
import { SubscriptionPlan } from '../../types/subscription.interface';

@Component({
  selector: 'app-upgrade-modal',
  standalone: true,
  imports: [CommonModule, ModalComponent, ButtonComponent, AlertComponent],
  templateUrl: './upgrade-modal.component.html',
  styleUrls: ['./upgrade-modal.component.scss']
})
export class UpgradeModalComponent {
  @Input() isOpen = false;
  @Input() plan: SubscriptionPlan | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() upgradeSuccess = new EventEmitter<void>();

  isUpgrading = false;
  upgradeError = '';
  isUpgradeComplete = false; // Renamed from upgradeSuccess to avoid conflict

  constructor(private subscriptionService: SubscriptionService) {}

  ngOnChanges(): void {
    if (this.isOpen) {
      this.resetState();
    }
  }

  getModalTitle(): string {
    if (this.isUpgradeComplete) return 'Upgrade Complete';
    return this.plan ? `Upgrade to ${this.plan.name}` : 'Upgrade Plan';
  }

  getKeyFeatures(): string[] {
    if (!this.plan) return [];
    // Return first 3 features excluding the scan limit
    return this.plan.features.slice(1, 4);
  }

  confirmUpgrade(): void {
    if (!this.plan) return;

    this.isUpgrading = true;
    this.upgradeError = '';

    this.subscriptionService.upgradePlan({
      planId: this.plan.id,
      interval: this.plan.interval
    }).subscribe({
      next: (success) => {
        this.isUpgrading = false;
        if (success) {
          this.isUpgradeComplete = true;
        }
      },
      error: (error) => {
        this.isUpgrading = false;
        this.upgradeError = 'Failed to process upgrade. Please try again or contact support.';
        console.error('Upgrade failed:', error);
      }
    });
  }

  onUpgradeComplete(): void {
    this.upgradeSuccess.emit();
    this.close.emit();
  }

  private resetState(): void {
    this.isUpgrading = false;
    this.upgradeError = '';
    this.isUpgradeComplete = false;
  }
}