import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SubjectService, SubjectDto, TeacherSubjectDto, CreateTeacherSubjectDto } from '../../../services/subject.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-subject-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subject-selector.component.html',
  styleUrls: ['./subject-selector.component.scss']
})
export class SubjectSelectorComponent implements OnInit, OnDestroy {
  private subjectService = inject(SubjectService);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  allSubjects: SubjectDto[] = [];
  teacherSubjects: TeacherSubjectDto[] = [];
  selectedSubjectId: string = '';
  pricePerHour: number = 0;
  loading = false;
  teacherId: string = '';
  confirmingDeleteId: string | null = null;

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (user?.id) {
          this.teacherId = user.id;
          this.loadSubjects();
          this.loadTeacherSubjects();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadSubjects(): void {
    this.subjectService.getAllSubjects()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (subjects) => {
          this.allSubjects = subjects;
        },
        error: (error) => {
          this.toastService.error('Erreur lors du chargement des matières');
          console.error('Error loading subjects:', error);
        }
      });
  }

  loadTeacherSubjects(): void {
    if (!this.teacherId) return;

    this.subjectService.getTeacherSubjects(this.teacherId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (subjects) => {
          this.teacherSubjects = subjects;
        },
        error: (error) => {
          this.toastService.error('Erreur lors du chargement de vos matières');
          console.error('Error loading teacher subjects:', error);
        }
      });
  }

  addSubject(): void {
    if (!this.selectedSubjectId) {
      this.toastService.error('Veuillez sélectionner une matière');
      return;
    }

    if (this.pricePerHour <= 0) {
      this.toastService.error('Veuillez entrer un prix valide');
      return;
    }

    const dto: CreateTeacherSubjectDto = {
      subjectId: this.selectedSubjectId,
      pricePerHour: this.pricePerHour
    };

    this.loading = true;
    this.subjectService.createTeacherSubject(this.teacherId, dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newSubject) => {
          this.teacherSubjects.push(newSubject);
          this.toastService.success('Matière ajoutée avec succès');
          this.selectedSubjectId = '';
          this.pricePerHour = 0;
          this.loading = false;
        },
        error: (error) => {
          this.toastService.error('Erreur lors de l\'ajout de la matière');
          console.error('Error adding subject:', error);
          this.loading = false;
        }
      });
  }

  confirmRemoveSubject(subjectId: string): void {
    this.confirmingDeleteId = subjectId;
  }

  cancelRemove(): void {
    this.confirmingDeleteId = null;
  }

  removeSubject(subjectId: string): void {
    this.subjectService.deleteTeacherSubject(this.teacherId, subjectId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.teacherSubjects = this.teacherSubjects.filter(s => s.subjectId !== subjectId);
          this.toastService.success('Matière retirée avec succès');
          this.confirmingDeleteId = null;
        },
        error: (error) => {
          this.toastService.error('Erreur lors de la suppression de la matière');
          console.error('Error removing subject:', error);
          this.confirmingDeleteId = null;
        }
      });
  }

  getAvailableSubjects(): SubjectDto[] {
    const teacherSubjectIds = this.teacherSubjects.map(ts => ts.subjectId);
    return this.allSubjects.filter(s => !teacherSubjectIds.includes(s.id));
  }

  isSubjectSelected(subjectId: string): boolean {
    return this.teacherSubjects.some(ts => ts.subjectId === subjectId);
  }
}
