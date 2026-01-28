import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, ChatDto, ChatDetailDto, MessageDto, CreateChatDto, ChatType, TeacherDto } from '../../services/chat.service';
import { ChatSignalRService } from '../../services/chat-signalr.service';
import { AuthService, ProfileType, UserDto } from '../../services/auth.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy {
  chats: ChatDto[] = [];
  selectedChat: ChatDetailDto | null = null;
  currentUser: UserDto | null = null;
  ProfileType = ProfileType;
  ChatType = ChatType;
  Array = Array;
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
  typingUsers: Set<string> = new Set();
  isSignalRConnected = false;

  constructor(
    private chatService: ChatService,
    private chatSignalRService: ChatSignalRService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        // Connect first, then load chats so joins succeed
        this.chatSignalRService.connect()
          .then(() => this.loadChats())
          .catch(err => {
            console.error('Failed to connect to SignalR:', err);
            this.loadChats(); // fallback to at least fetch data
          });
      }
    });

    // Subscribe to incoming messages
    this.chatSignalRService.messageReceived$.subscribe(message => {
      if (!message) return;

      // Update chat list last message + unread count
      const idx = this.chats.findIndex(c => c.chatId === message.chatId);
      if (idx >= 0) {
        const chat = this.chats[idx];
        const isActiveChat = this.selectedChat && this.selectedChat.chatId === message.chatId;
        const unread = isActiveChat ? 0 : (chat.unreadCount ?? 0) + 1;
        this.chats[idx] = {
          ...chat,
          lastMessage: message.content,
          lastMessageTime: message.createdAt,
          unreadCount: unread
        } as ChatDto;
      }

      if (this.selectedChat && message.chatId === this.selectedChat.chatId) {
        // Add message to current chat
        this.selectedChat.messages.push(message);
      }
    });

    // Subscribe to typing notifications
    this.chatSignalRService.userTyping$.subscribe(userName => {
      if (userName) {
        this.typingUsers.add(userName);
      }
    });

    this.chatSignalRService.userStoppedTyping$.subscribe(() => {
      this.typingUsers.clear();
    });

    // Subscribe to connection status
    this.chatSignalRService.connectionStatus$.subscribe(status => {
      this.isSignalRConnected = status;

      // On reconnect, rejoin all chats to receive messages
      if (status) {
        this.chats.forEach(c => {
          this.chatSignalRService.joinChat(c.chatId).catch(err => {
            console.error('Error rejoining chat group', c.chatId, err);
          });
        });
      }
    });
  }

  ngOnDestroy(): void {
    // Leave current chat and disconnect SignalR
    if (this.selectedChat) {
      this.chatSignalRService.leaveChat(this.selectedChat.chatId).catch(err => {
        console.error('Error leaving chat:', err);
      });
    }
    this.chatSignalRService.disconnect().catch(err => {
      console.error('Error disconnecting from SignalR:', err);
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

        // Join all chat groups so incoming messages arrive even when not selected
        chats.forEach(c => {
          this.chatSignalRService.joinChat(c.chatId).catch(err => {
            console.error('Error joining chat group', c.chatId, err);
          });
        });
      },
      error: (err) => {
        this.error = 'Impossible de charger les discussions';
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
        this.error = 'Impossible de charger les professeurs';
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
    // Leave previous chat
    if (this.selectedChat) {
      this.chatSignalRService.leaveChat(this.selectedChat.chatId).catch(err => {
        console.error('Error leaving previous chat:', err);
      });
    }

    this.loading = true;
    this.chatService.getChatDetail(chat.chatId).subscribe({
      next: (chatDetail) => {
        this.selectedChat = chatDetail;
        this.messageContent = '';
        this.loading = false;
        this.typingUsers.clear();

        // reset unread locally
        const idx = this.chats.findIndex(c => c.chatId === chat.chatId);
        if (idx >= 0) {
          this.chats[idx].unreadCount = 0;
        }

        // mark as read server-side
        this.chatService.markChatAsRead(chat.chatId).subscribe({
          error: (err) => console.error('Failed to mark chat as read', err)
        });

        // Join chat group via SignalR
        this.chatSignalRService.joinChat(chatDetail.chatId).catch(err => {
          console.error('Error joining chat:', err);
        });
      },
      error: (err) => {
        this.error = 'Impossible de charger la discussion';
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
    const messageContent = this.messageContent;
    this.chatService.sendMessage(this.selectedChat.chatId, { Content: messageContent }).subscribe({
      next: (message) => {
        if (this.selectedChat) {
          this.messageContent = '';
          this.loading = false;
          this.typingUsers.clear();

          // update last message + reset unread for this chat in list
          const idx = this.chats.findIndex(c => c.chatId === this.selectedChat!.chatId);
          if (idx >= 0) {
            this.chats[idx] = {
              ...this.chats[idx],
              lastMessage: message.content,
              lastMessageTime: message.createdAt,
              unreadCount: 0
            } as ChatDto;
          }

          // Broadcast message via SignalR (will add to messages for all users including sender)
          this.chatSignalRService.sendMessageToChat(this.selectedChat.chatId, message).catch(err => {
            console.error('Error sending message via SignalR:', err);
          });

          // Notify that user stopped typing
          if (this.currentUser) {
            this.chatSignalRService.notifyStoppedTyping(this.selectedChat.chatId).catch(err => {
              console.error('Error notifying stopped typing:', err);
            });
          }
        }
      },
      error: (err) => {
        this.error = 'Impossible d\'envoyer le message';
        console.error(err);
        this.loading = false;
      }
    });
  }

  createNewChat(): void {
    if (!this.newChatTeacherId.trim()) {
      this.error = 'Veuillez sélectionner un professeur';
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
        this.error = err.error?.message || 'Impossible de créer la discussion';
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

    if (confirm('Êtes-vous sûr de vouloir archiver cette discussion ?')) {
      this.chatService.archiveChat(this.selectedChat.chatId).subscribe({
        next: () => {
          this.chats = this.chats.filter(c => c.chatId !== this.selectedChat?.chatId);
          this.selectedChat = null;
        },
        error: (err) => {
          this.error = 'Impossible d\'archiver la discussion';
          console.error(err);
        }
      });
    }
  }

  // Debounce typing notifications
  private typingTimeout: any;
  
  onMessageInput(): void {
    if (!this.selectedChat || !this.currentUser) {
      return;
    }

    // Clear existing timeout
    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }

    // Notify that user is typing
    this.chatSignalRService.notifyTyping(this.selectedChat.chatId, this.currentUser.firstName || 'Utilisateur').catch(err => {
      console.error('Error notifying typing:', err);
    });

    // Set timeout to notify that user stopped typing
    this.typingTimeout = setTimeout(() => {
      this.chatSignalRService.notifyStoppedTyping(this.selectedChat!.chatId).catch(err => {
        console.error('Error notifying stopped typing:', err);
      });
    }, 2000);
  }
}

