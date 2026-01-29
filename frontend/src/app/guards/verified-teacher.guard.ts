import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, take } from 'rxjs/operators';

export const verifiedTeacherGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.currentUser$.pipe(
    take(1),
    map(user => {
      if (!user) {
        router.navigate(['/login']);
        return false;
      }

      // If user is a teacher, check verification status
      if (user.profileType === 1) { // 1 = Teacher
        // Teachers must be verified (status 1) or diploma verified (status 3)
        const isVerified = user.verificationStatus === 1 || user.verificationStatus === 3;
        
        if (!isVerified) {
          router.navigate(['/documents'], {
            queryParams: { message: 'verification-required' }
          });
          return false;
        }
        return true;
      }

      // Non-teachers (Parent, Student, Admin) can access without verification
      // but they shouldn't access teacher-specific planning pages
      if (state.url.includes('/teacher/planning')) {
        router.navigate(['/']);
        return false;
      }

      return true;
    })
  );
};

export const teacherOnlyGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.currentUser$.pipe(
    take(1),
    map(user => {
      if (!user) {
        router.navigate(['/login']);
        return false;
      }

      // Allow both teachers (1) and admins (0)
      if (user.profileType !== 1 && user.profileType !== 0) {
        router.navigate(['/']);
        return false;
      }

      return true;
    })
  );
};
