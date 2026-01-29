import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ParentService, TeacherStatsDto } from '../../../services/parent.service'; // Adjust path
import { SmallIconComponent } from '../SmallIcon/small-icon.component'; // Adjust path
import { ToastService } from '../../../services/toast.service'; // Adjust path

@Component({
  selector: 'app-teacher-reviews',
  standalone: true,
  imports: [CommonModule, SmallIconComponent],
  templateUrl: './teacher-reviews.component.html',
  styleUrls: ['./teacher-reviews.component.scss']
})
export class TeacherReviewsComponent implements OnInit {
  @Input() teacherId!: string | null;

  private parentService = inject(ParentService);
  private toastService = inject(ToastService);

  stats: TeacherStatsDto | null = null;
  loading: boolean = true;
  error: boolean = false;

  // Helper to generate an array for star display (e.g. 4.5 -> [1,1,1,1,0.5])
  // We'll simplify for now: full stars only based on rounded rating
  get starsArray(): number[] {
    if (!this.stats) return [];
    const fullStars = Math.round(this.stats.averageRating);
    return Array(5).fill(0).map((_, i) => i < fullStars ? 1 : 0);
  }

  ngOnInit() {
    if (!this.teacherId) {
      this.loading = false;
      return;
    }
    this.parentService.getTeacherStats(this.teacherId).subscribe({
      next: (data) => {
        this.stats = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching teacher stats', err);
        this.toastService.error('Impossible de charger les avis.');
        this.error = true;
        this.loading = false;
      }
    });
  }

// Dans teacher-reviews.component.ts
  getStarColor(rating: number): string {
    if (rating >= 4) return '#4EE381'; // Jaune
    if (rating >= 2.5) return '#f97316'; // Orange
    return '#ef4444'; // Rouge
  }
}