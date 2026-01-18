import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private token: string | null = null;
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
      // Fallback: Versuchen, Token aus Storage zu laden
      this.token = localStorage.getItem('cis_token');
    }

    // 3. Rolle auslesen (?role=lecturer oder ?role=student)
    const urlRole = urlParams.get('role');
    if (urlRole === 'lecturer' || urlRole === 'student') {
      this.role = urlRole;
      console.log('Benutzer-Rolle erkannt:', this.role);
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
