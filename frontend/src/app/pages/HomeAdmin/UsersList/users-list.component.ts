import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

// Components
import { HeaderComponent } from '../../../components/Header/header.component';
import { TextInputComponent } from '../../../components/shared/TextInput/text-input.component';
import { SelectComponent, SelectOption } from '../../../components/shared/Select/select.component';
import { IconComponent } from '../../../components/shared/Icon/icon.component';
import { SmallIconComponent } from '../../../components/shared/SmallIcon/small-icon.component';

// Services
import { UserService } from '../../../services/user.service';
import { ProfileType, UserDto } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service'; // Assurez-vous que le chemin est correct

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TextInputComponent,
    SelectComponent,
    IconComponent,
    SmallIconComponent,
    HeaderComponent
  ],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent implements OnInit {
  private toastService = inject(ToastService);

  // Data
  users: UserDto[] = [];
  filteredUsers: UserDto[] = [];
  isLoading = true;

  ProfileType = ProfileType;

  // Filters
  searchQuery: string = '';
  selectedRole: string | number = 'all';
  selectedStatus: string | number = 'all';

  // Options for Select Components
  roleOptions: SelectOption[] = [
    { label: 'Tous les rôles', value: 'all' },
    { label: 'Professeurs', value: ProfileType.Teacher },
    { label: 'Parents', value: ProfileType.Parent },
    { label: 'Élèves', value: ProfileType.Student },
    { label: 'Administrateurs', value: ProfileType.Admin },
  ];

  statusOptions: SelectOption[] = [
    { label: 'Tous les statuts', value: 'all' },
    { label: 'Actif', value: 'active' },
    { label: 'Suspendu / Inactif', value: 'inactive' }
  ];

  constructor(
    private userService: UserService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.applyFilters();
        this.isLoading = false;
        // Optionnel : Un toast info si la liste est vide au chargement
        if (data.length === 0) {
          this.toastService.info('Aucun utilisateur trouvé dans la base de données.');
        }
      },
      error: (err) => {
        console.error('Error loading users', err);
        this.isLoading = false;
        // 2. Toast d'erreur lors du chargement
        this.toastService.error('Impossible de charger la liste des utilisateurs. Vérifiez votre connexion.');
      }
    });
  }

  // --- Filtering Logic ---
  applyFilters(): void {
    this.filteredUsers = this.users.filter(user => {
      // 1. Text Search (Name or Email)
      const matchesSearch = !this.searchQuery || 
        (user.firstName?.toLowerCase().includes(this.searchQuery.toLowerCase()) || '') ||
        (user.lastName?.toLowerCase().includes(this.searchQuery.toLowerCase()) || '') ||
        (user.email?.toLowerCase().includes(this.searchQuery.toLowerCase()) || '') ||
        (user.username?.toLowerCase().includes(this.searchQuery.toLowerCase()) || '');

      // 2. Role Filter
      const matchesRole = this.selectedRole === 'all' || user.profileType === Number(this.selectedRole);

      // 3. Status Filter
      const matchesStatus = this.selectedStatus === 'all' || 
        (this.selectedStatus === 'active' ? user.isActive : !user.isActive);

      return matchesSearch && matchesRole && matchesStatus;
    });
  }

  onSearchChange(query: string): void {
    this.searchQuery = query;
    this.applyFilters();
  }

  onRoleChange(role: string | number | null): void {
    if (role !== null) {
      this.selectedRole = role;
      this.applyFilters();
    }
  }

  onStatusChange(status: string | number | null): void {
    if (status !== null) {
      this.selectedStatus = status;
      this.applyFilters();
    }
  }

  // --- Actions ---

  viewUser(id: string): void {
    this.router.navigate(['/admin/users', id]);
  }

  openChat(id: string): void {
    this.router.navigate(['/admin/support'], { queryParams: { userId: id } });
  }

// In users-list.component.ts

deleteUser(user: UserDto): void {
  if (confirm(`Êtes-vous sûr de vouloir supprimer l'utilisateur ${user.firstName} ${user.lastName} ?`)) {
    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.users = this.users.filter(u => u.id !== user.id);
        this.applyFilters();
        this.toastService.success(`L'utilisateur ${user.firstName} ${user.lastName} a été supprimé avec succès.`);
      },
      error: (err) => {
        console.error('DELETE ERROR DETAILS:', err.error); // <--- Look at this in the console!
        // It often contains { title: "...", detail: "Cannot delete user because..." }
        this.toastService.error(`Erreur: ${err.error?.detail || 'Suppression impossible.'}`);
      }
    });
  }
}

  // --- Helpers for Template ---

  getRoleLabel(profile: ProfileType): string {
    return this.userService.getProfileLabel(profile);
  }

  getRoleClass(profile: ProfileType): string {
    switch (profile) {
      case ProfileType.Admin: return 'role-admin';
      case ProfileType.Teacher: return 'role-teacher';
      case ProfileType.Parent: return 'role-parent';
      case ProfileType.Student: return 'role-student';
      default: return '';
    }
  }
}