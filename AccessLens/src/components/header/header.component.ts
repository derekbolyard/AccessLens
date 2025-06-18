import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../common/button/button.component';
import { ModalComponent } from '../common/modal/modal.component';
import { InputComponent } from '../common/input/input.component';
import { AlertComponent } from '../common/alert/alert.component';
import { AuthModalComponent } from '../auth/auth-modal.component';
import { FeedbackModalComponent } from '../support/feedback-modal.component';
import { UpgradeModalComponent } from '../upgrade/upgrade-modal.component';
import { ReportService } from '../../services/report.service';
import { User } from '../../services/auth.service';
import { AuthService } from '../../services/auth.service';
import { SupportService } from '../../services/support.service';
import { SubscriptionService } from '../../services/subscription.service';
import { UserSubscription, SubscriptionPlan } from '../../types/subscription.interface';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule, 
    ButtonComponent, 
    ModalComponent, 
    InputComponent, 
    AlertComponent,
    AuthModalComponent,
    FeedbackModalComponent,
    UpgradeModalComponent
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  showScanModal = false;
  showAuthModal = false;
  showFeedbackModal = false;
  showUpgradeModal = false;
  showUserMenu = false;
  scanUrl = '';
  isScanning = false;
  scanError = '';
  scanSuccess = false;
  currentUser: User | null = null;
  currentSubscription: UserSubscription | null = null;
  isSigningOut = false;

  constructor(
    private reportService: ReportService,
    private authService: AuthService,
    private supportService: SupportService,
    public subscriptionService: SubscriptionService,
    private router: Router
  ) {
    this.authService.user$.subscribe((user: any) => {
      this.currentUser = user;
    });

    this.subscriptionService.getCurrentSubscription().subscribe(subscription => {
      this.currentSubscription = subscription;
    });
  }

  get proPlan(): SubscriptionPlan | null {
    return this.subscriptionService.getPlans().find(p => p.id === 'pro') ?? null;
  }

  onLogoClick(): void {
    this.router.navigate(['/dashboard']);
  }

  onDashboardClick(): void {
    this.router.navigate(['/dashboard']);
  }

  onSitesClick(): void {
    this.router.navigate(['/sites']);
  }

  onBrandingClick(): void {
    this.router.navigate(['/branding']);
  }

  onUpgradeClick(): void {
    this.router.navigate(['/upgrade']);
    this.showUserMenu = false;
  }

  onRequestScan(): void {
    if (!this.currentUser) {
      this.showAuthModal = true;
      return;
    }

    // Check if user has scans remaining
    if (!this.subscriptionService.canRequestScan()) {
      this.showUpgradeModal = true;
      return;
    }

    this.showScanModal = true;
    this.resetScanState();
  }

  onSignIn(): void {
    this.showAuthModal = true;
  }

  onSignOut(): void {
    this.isSigningOut = true;
    this.authService.signOut().subscribe({
      next: () => {
        this.isSigningOut = false;
        this.showUserMenu = false;
      },
      error: (error: any) => {
        this.isSigningOut = false;
        console.error('Sign out failed:', error);
      }
    });
  }

  onFeedback(): void {
    this.showFeedbackModal = true;
    this.showUserMenu = false;
  }

  onContactSupport(): void {
    this.supportService.openEmailClient('Support Request');
    this.showUserMenu = false;
  }

  onCloseScanModal(): void {
    this.showScanModal = false;
    this.resetScanState();
  }

  onCloseAuthModal(): void {
    this.showAuthModal = false;
  }

  onCloseFeedbackModal(): void {
    this.showFeedbackModal = false;
  }

  onCloseUpgradeModal(): void {
    this.showUpgradeModal = false;
  }

  onAuthSuccess(user: User): void {
    this.currentUser = user;
    // If they were trying to scan, open the scan modal
    if (!this.showScanModal) {
      this.onRequestScan();
    }
  }

  onUpgradeSuccess(): void {
    this.showUpgradeModal = false;
    // After successful upgrade, allow them to scan
    this.onRequestScan();
  }

  toggleUserMenu(): void {
    this.showUserMenu = !this.showUserMenu;
  }

  getScansRemaining(): number {
    return this.subscriptionService.getScansRemaining();
  }

  onSubmitScan(): void {
    if (!this.scanUrl.trim()) {
      this.scanError = 'Please enter a valid URL';
      return;
    }

    if (!this.isValidUrl(this.scanUrl)) {
      this.scanError = 'Please enter a valid URL (must start with http:// or https://)';
      return;
    }

    if (!this.subscriptionService.canRequestScan()) {
      this.scanError = 'You have reached your scan limit for this month. Please upgrade your plan.';
      return;
    }

    this.isScanning = true;
    this.scanError = '';

    this.reportService.requestNewScan(this.scanUrl).subscribe({
      next: (success) => {
        this.isScanning = false;
        if (success) {
          // Increment scan usage
          this.subscriptionService.incrementScanUsage();
          this.scanSuccess = true;
          setTimeout(() => {
            this.onCloseScanModal();
          }, 2000);
        } else {
          this.scanError = 'Failed to start scan. Please try again.';
        }
      },
      error: (error) => {
        this.isScanning = false;
        this.scanError = 'An error occurred while starting the scan. Please try again.';
        console.error('Scan request failed:', error);
      }
    });
  }

  private resetScanState(): void {
    this.scanUrl = '';
    this.isScanning = false;
    this.scanError = '';
    this.scanSuccess = false;
  }

  private isValidUrl(url: string): boolean {
    try {
      const urlObj = new URL(url);
      return urlObj.protocol === 'http:' || urlObj.protocol === 'https:';
    } catch {
      return false;
    }
  }
}