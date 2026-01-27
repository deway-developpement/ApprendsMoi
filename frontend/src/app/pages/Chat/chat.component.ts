import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, ChatDto, ChatDetailDto, MessageDto, CreateChatDto, ChatType, TeacherDto } from '../../services/chat.service';
import { AuthService, ProfileType, UserDto } from '../../services/auth.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {
  chats: ChatDto[] = [];
  selectedChat: ChatDetailDto | null = null;
  currentUser: UserDto | null = null;
  ProfileType = ProfileType;
  ChatType = ChatType;
  teachers: TeacherDto[] = [];
  filteredTeachers: TeacherDto[] = [];

  // Form state
  showCreateChatModal = false;
  newChatTeacherId = '';
  teacherSearchTerm = '';
  loadingTeachers = false;
  messageContent = '';
  loading = false;
  error = '';

  constructor(
    private chatService: ChatService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.loadChats();
      }
    });
  }

  openCreateChatModal(): void {
    this.showCreateChatModal = true;
    if (this.teachers.length === 0) {
      this.loadTeachers();
    } else {
      this.applyTeacherFilter();
    }
  }

  loadChats(): void {
    this.loading = true;
    this.chatService.getChats().subscribe({
      next: (chats) => {
        this.chats = chats;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load chats';
        console.error(err);
        this.loading = false;
      }
    });
  }

  loadTeachers(searchTerm: string = ''): void {
    this.loadingTeachers = true;
    this.chatService.searchTeachers(searchTerm).subscribe({
      next: (teachers) => {
        this.teachers = teachers;
        this.filteredTeachers = teachers;
        this.loadingTeachers = false;
      },
      error: (err) => {
        this.error = 'Failed to load teachers';
        console.error(err);
        this.loadingTeachers = false;
      }
    });
  }

  applyTeacherFilter(): void {
    const term = this.teacherSearchTerm.trim();
    this.loadTeachers(term);
  }

  selectTeacher(teacher: TeacherDto): void {
    this.newChatTeacherId = teacher.id;
  }

  selectChat(chat: ChatDto): void {
    this.loading = true;
    this.chatService.getChatDetail(chat.chatId).subscribe({
      next: (chatDetail) => {
        this.selectedChat = chatDetail;
        this.messageContent = '';
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load chat';
        console.error(err);
        this.loading = false;
      }
    });
  }

  sendMessage(): void {
    if (!this.selectedChat || !this.messageContent.trim()) {
      return;
    }

    this.loading = true;
    this.chatService.sendMessage(this.selectedChat.chatId, { Content: this.messageContent }).subscribe({
      next: (message) => {
        if (this.selectedChat) {
          this.selectedChat.messages.push(message);
          this.messageContent = '';
          this.loading = false;
        }
      },
      error: (err) => {
        this.error = 'Failed to send message';
        console.error(err);
        this.loading = false;
      }
    });
  }

  createNewChat(): void {
    if (!this.newChatTeacherId.trim()) {
      this.error = 'Please select a teacher';
      return;
    }

    const dto: CreateChatDto = {
      chatType: ChatType.ParentChat,
      teacherId: this.newChatTeacherId
    };

    this.loading = true;
    this.chatService.createChat(dto).subscribe({
      next: (chat) => {
        this.chats.unshift(chat);
        this.newChatTeacherId = '';
        this.showCreateChatModal = false;
        this.loading = false;
        this.selectChat(chat);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create chat';
        console.error(err);
        this.loading = false;
      }
    });
  }

  closeCreateModal(): void {
    this.showCreateChatModal = false;
    this.newChatTeacherId = '';
    this.teacherSearchTerm = '';
    this.filteredTeachers = this.teachers;
    this.error = '';
  }

  archiveChat(): void {
    if (!this.selectedChat) return;

    if (confirm('Are you sure you want to archive this chat?')) {
      this.chatService.archiveChat(this.selectedChat.chatId).subscribe({
        next: () => {
          this.chats = this.chats.filter(c => c.chatId !== this.selectedChat?.chatId);
          this.selectedChat = null;
        },
        error: (err) => {
          this.error = 'Failed to archive chat';
          console.error(err);
        }
      });
    }
  }
}
