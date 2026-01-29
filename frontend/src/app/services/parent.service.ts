import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of } from 'rxjs';
import { environment } from '../environments/environment';
import { GradeLevel } from './auth.service'; // On réutilise l'enum défini précédemment

// --- INTERFACES UI (Ce que le composant attend) ---

export interface Child {
  id: string; // string car UUID dans le back
  firstName: string;
  lastName: string;
  levelLabel: string; // Ex: "CM2" (transformé depuis l'enum)
  gradeLevel: GradeLevel; // La valeur brute pour l'API
  avatarColor: string; // Généré côté front
  username: string;
}

export interface CreateChildRequest {
  firstName: string;
  lastName: string; // On le demandera ou on prendra celui du parent
  password: string;
  gradeLevel: GradeLevel;
  birthDate?: String; // Optionnel
}

// --- INTERFACES API (Ce que Swagger renvoie) ---

export interface StudentDto {
  id: string;
  firstName: string;
  lastName: string;
  username: string;
  gradeLevel: GradeLevel;
  profile: number;
}

export interface TeacherRating {
  rating: number;
  comment: string;
}

export interface TeacherStatsDto {
  teacherId: string;
  teacherName: string;
  averageRating: number;
  totalRatings: number;
  ratingDistribution: { [key: string]: number }; // Handles "additionalProp1": 0, etc.
  recentRatings: TeacherRating[];
}

@Injectable({
  providedIn: 'root'
})
export class ParentService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // Palette de couleurs pour les avatars (pour éviter le gris triste)
  private readonly avatarColors = [
    '#F59E0B', // Amber
    '#10B981', // Emerald
    '#3B82F6', // Blue
    '#8B5CF6', // Violet
    '#EC4899', // Pink
    '#F43F5E'  // Rose
  ];

  constructor() {}

  // --- 1. GESTION DES ENFANTS ---

  /**
   * Récupère la liste des enfants depuis l'API et formate les données pour l'affichage
   * Endpoint: GET /api/Users/students
   */
  getMyChildren(): Observable<Child[]> {
    return this.http.get<StudentDto[]>(`${this.apiUrl}/api/Users/students`).pipe(
      map(students => students.map(student => ({
        id: student.id,
        firstName: student.firstName,
        lastName: student.lastName,
        username: student.username,
        gradeLevel: student.gradeLevel,
        // Logique métier : Transformation Enum -> Texte
        levelLabel: this.getGradeLabel(student.gradeLevel),
        // Logique UI : Attribution d'une couleur unique basée sur l'ID ou le nom
        avatarColor: this.getColorForString(student.firstName)
      })))
    );
  }

  /**
   * Inscrit un nouvel élève lié au parent connecté
   * Endpoint: POST /api/Auth/register/student
   */
  addChild(data: CreateChildRequest): Observable<any> {
    // Logique métier : Génération d'un username unique si non fourni
    // Ex: Lucas -> lucas.dubois.123
    const randomSuffix = Math.floor(Math.random() * 100);
    const generatedUsername = `${data.firstName.toLowerCase()}_${data.lastName.toLowerCase()}_${randomSuffix}`;

    const payload = {
      firstName: data.firstName,
      lastName: data.lastName,
      password: data.password,
      gradeLevel: data.gradeLevel,
      username: generatedUsername,
      birthDate: data.birthDate, // Optionnel
    };

    return this.http.post(`${this.apiUrl}/api/Auth/register/student`, payload);
  }

  // --- 2. MOCK DATA (En attendant que le Back soit prêt) ---
  
  // Ces méthodes simulent les appels API pour les parties manquantes du Swagger
  
  getUpcomingCourses(): Observable<any[]> {
    // TODO: Remplacer par GET /api/Courses quand disponible
    return of([
      { id: 101, date: new Date('2023-11-20T14:00:00'), tutorName: 'Julie B.', subject: 'Maths', childName: 'Léa', mode: 'Domicile', status: 'Confirmé', price: 35 },
      { id: 102, date: new Date('2023-11-22T10:00:00'), tutorName: 'Marc D.', subject: 'Français', childName: 'Thomas', mode: 'Visio', status: 'En attente', price: 25 }
    ]);
  }

  getLastPayment(): Observable<any> {
    // TODO: Remplacer par GET /api/Payments/last quand disponible
    return of({
      amount: 35,
      status: 'Payé',
      date: new Date()
    });
  }

  getLastMessage(): Observable<any> {
     // TODO: Remplacer par GET /api/Messages/last quand disponible
    return of({
      sender: 'Julie B.',
      preview: "N'oublie pas l'exercice de géométrie pour mercredi !",
      date: new Date()
    });
  }

  // --- HELPER FUNCTIONS ---

  /**
   * Convertit l'enum (0, 1, 2) en libellé lisible (CP, CE1...)
   */
  getGradeLabel(grade: GradeLevel): string {
    const labels: Record<number, string> = {
      [GradeLevel.CP]: 'CP',
      [GradeLevel.CE1]: 'CE1',
      [GradeLevel.CE2]: 'CE2',
      [GradeLevel.CM1]: 'CM1',
      [GradeLevel.CM2]: 'CM2',
      [GradeLevel.Sixieme]: '6ème',
      [GradeLevel.Cinquieme]: '5ème',
      [GradeLevel.Quatrieme]: '4ème',
      [GradeLevel.Troisieme]: '3ème',
      [GradeLevel.Seconde]: '2nde',
      [GradeLevel.Premiere]: '1ère',
      [GradeLevel.Terminale]: 'Terminale'
    };
    return labels[grade] || 'Niveau inconnu';
  }

  /**
   * Génère une couleur constante à partir d'une chaîne de caractères
   */
  private getColorForString(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash % this.avatarColors.length);
    return this.avatarColors[index];
  }

  deleteChild(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/Users/${id}`);
  }

  getTeacherStats(teacherId: string): Observable<TeacherStatsDto> {
    return this.http.get<TeacherStatsDto>(`${this.apiUrl}/api/Ratings/stats/teacher/${teacherId}`);
  }
}