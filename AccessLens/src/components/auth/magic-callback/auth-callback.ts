import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service'; // Change this!

@Component({
  selector: 'app-auth-callback',
  templateUrl: './auth-callback.html',
  styleUrls: ['./auth-callback.scss']
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const fragment = this.route.snapshot.fragment;
    if (fragment && fragment.startsWith('token=')) {
      const token = fragment.substring(6);
      this.authService.handleAuthCallback(token); // Change this!
      
      const returnUrl = localStorage.getItem('auth_return_url') || '/dashboard';
      localStorage.removeItem('auth_return_url');
      this.router.navigate([returnUrl]);
    } else {
      this.router.navigate(['/']);
    }
  }
}