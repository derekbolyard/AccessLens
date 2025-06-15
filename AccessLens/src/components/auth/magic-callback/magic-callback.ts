import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-magic-callback',
  imports: [],
  templateUrl: './magic-callback.html',
  styleUrl: './magic-callback.scss'
})
export class MagicCallbackComponent implements OnInit {
  constructor(private router: Router) {}

  ngOnInit() {
    // Extract token from URL fragment
    const fragment = window.location.hash.substring(1);
    const params = new URLSearchParams(fragment);
    const token = params.get('token');

    if (token) {
      // Store token in localStorage or your auth service
      localStorage.setItem('access_token', token);
      
      // Redirect to dashboard
      this.router.navigate(['/dashboard']);
    } else {
      // Redirect to login on error
      this.router.navigate(['/auth/login']);
    }
  }
}