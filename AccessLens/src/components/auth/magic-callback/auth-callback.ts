import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  templateUrl: './auth-callback.html',
  styleUrls: ['./auth-callback.scss']
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private magicAuthService: AuthService
  ) {}

  ngOnInit(): void {
    // Get token from URL fragment
    const fragment = this.route.snapshot.fragment;
    if (fragment && fragment.startsWith('token=')) {
      const token = fragment.substring(6); // Remove 'token='
      this.magicAuthService.handleAuthCallback(token);
      
      // Redirect to dashboard or original destination
      const returnUrl = localStorage.getItem('auth_return_url') || '/dashboard';
      localStorage.removeItem('auth_return_url');
      this.router.navigate([returnUrl]);
    } else {
      // No token found, redirect to sign in
      this.router.navigate(['/']);
    }
  }
}