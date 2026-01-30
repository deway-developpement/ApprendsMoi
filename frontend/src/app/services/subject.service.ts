import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface SubjectDto {
  id: string;
  name: string;
  slug: string;
}

export interface TeacherSubjectDto {
  teacherId: string;
  teacherName: string;
  subjectId: string;
  subjectName: string;
  pricePerHour: number;
}

export interface CreateTeacherSubjectDto {
  subjectId: string;
  pricePerHour: number;
}

export interface UpdateTeacherSubjectDto {
  pricePerHour?: number;
}

@Injectable({
  providedIn: 'root'
})
export class SubjectService {
  private http = inject(HttpClient);

  getAllSubjects(): Observable<SubjectDto[]> {
    return this.http.get<SubjectDto[]>(`${environment.apiUrl}/api/subjects`);
  }

  getTeacherSubjects(teacherId: string): Observable<TeacherSubjectDto[]> {
    return this.http.get<TeacherSubjectDto[]>(
      `${environment.apiUrl}/api/teachersubjects/teacher/${teacherId}`
    );
  }

  createTeacherSubject(teacherId: string, dto: CreateTeacherSubjectDto): Observable<TeacherSubjectDto> {
    return this.http.post<TeacherSubjectDto>(
      `${environment.apiUrl}/api/teachersubjects/teacher/${teacherId}`,
      dto
    );
  }

  updateTeacherSubject(
    teacherId: string,
    subjectId: string,
    dto: UpdateTeacherSubjectDto
  ): Observable<TeacherSubjectDto> {
    return this.http.put<TeacherSubjectDto>(
      `${environment.apiUrl}/api/teachersubjects/teacher/${teacherId}/subject/${subjectId}`,
      dto
    );
  }

  deleteTeacherSubject(teacherId: string, subjectId: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/api/teachersubjects/teacher/${teacherId}/subject/${subjectId}`
    );
  }
}
