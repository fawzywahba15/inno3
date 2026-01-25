import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private token: string | null = null;
  // Standard-Rolle ist 'student' (sicherer Fallback)
  private role: 'student' | 'lecturer' = 'student';

  constructor() {
    this.init();
  }

  private init() {
    // 1. schauen in die URL (Browser-Adresszeile)
    const urlParams = new URLSearchParams(window.location.search);

    // 2. Token auslesen (?token=XYZ)
    const urlToken = urlParams.get('token');

    if (urlToken) {
      this.token = urlToken;
      console.log('CIS Token gefunden:', this.token);
      // Token im LocalStorage speichern, falls der User F5 drückt
      localStorage.setItem('cis_token', this.token);
    } else {
      // Fallback: Versuchen, Token aus Storage zu laden (bei Reload)
      this.token = localStorage.getItem('cis_token');
    }

    // 3. SICHERHEITS-FIX: Rolle NICHT mehr aus URL lesen!
    // Stattdessen: Rolle anhand des Tokens bestimmen (Mock-Backend)
    if (this.token) {
      this.validateTokenAndSetRole(this.token);
    }
  }

  /**
   * Simuliert die Backend-Validierung.
   * Hier wird entschieden, wer welche Rechte hat.
   */
  private validateTokenAndSetRole(token: string) {
    if (token === 'teacher999') {
      this.role = 'lecturer';
      console.log('✅ Backend-Check: Token als LEHRENDER validiert.');
    }
    else if (token === 'student123') {
      this.role = 'student';
      console.log('✅ Backend-Check: Token als STUDENT validiert.');
    }
    else {
      // Unbekannter Token -> Fallback auf Student (oder ausloggen)
      this.role = 'student';
      console.warn('⚠️ Backend-Check: Unbekannter Token. Fallback auf Student.');
    }
  }

  getToken(): string | null {
    return this.token;
  }

  isLecturer(): boolean {
    return this.role === 'lecturer';
  }

  getUserRole(): string {
    return this.role; // 'student' oder 'lecturer'
  }
}
