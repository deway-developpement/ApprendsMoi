import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ProfileType, UserDto } from './auth.service';

// We reuse the UserDto from auth.service to ensure consistency across the app.
// If specific fields for the list view differ from the Auth UserDto, we can extend or redefine it here,
// but based on the provided swagger, they are compatible.

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly API_URL = environment.apiUrl;
  private readonly endpoint = `${this.API_URL}/api/Users`;

  constructor() {}

  getAllUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(this.endpoint);
  }

  getUserById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.endpoint}/${id}`);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`);
  }

  /**
   * Helper to map ProfileType enum to a readable string label.
   */
  getProfileLabel(type: ProfileType): string {
    switch (type) {
      case ProfileType.Admin: return 'Administrateur';
      case ProfileType.Teacher: return 'Professeur';
      case ProfileType.Parent: return 'Parent';
      case ProfileType.Student: return 'Élève';
      default: return 'Inconnu';
    }
  }
}