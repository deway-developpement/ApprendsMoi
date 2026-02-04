import { Component, ElementRef, ViewChild, OnInit, OnDestroy, AfterViewInit, AfterViewChecked, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { environment } from '../../environments/environment';
import { ChatService, ChatDto, ChatDetailDto, CreateChatDto, ChatType } from '../../services/chat.service';
import { ChatSignalRService } from '../../services/chat-signalr.service';
import { AuthService, ProfileType, UserDto } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

declare const ZoomMtgEmbedded: any;

interface MeetingDetailsResponse {
  meetingNumber: string;
  password: string;
  participantSignature: string;
  sdkKey: string;
  joinUrl: string;
  teacherId: string;
  studentId: string;
  courseId?: string | null;
}

interface CreateMeetingResponse extends MeetingDetailsResponse {}

interface RatingDto {
  id: string;
  teacherId: string;
  rating: number;
  comment?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

@Component({
  standalone: true,
  selector: 'app-visio',
  templateUrl: './Visio.component.html',
  styleUrls: ['./Visio.component.scss'],
  imports: [CommonModule, FormsModule, HeaderComponent, ButtonComponent]
})
export class Visio implements OnInit, OnDestroy, AfterViewInit, AfterViewChecked {
  @ViewChild('zoomContainer', { static: false }) zoomContainer?: ElementRef<HTMLDivElement>;
  @ViewChild('chatMessages', { static: false }) chatMessages?: ElementRef<HTMLDivElement>;

  private readonly http = inject(HttpClient);
  private readonly chatService = inject(ChatService);
  private readonly chatSignalRService = inject(ChatSignalRService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;
  private readonly coursesBaseUrl = `${environment.apiUrl}/api/courses`;
  private viewReady = false;
  
  courseId: string | null = null;
  meetingId: number | null = null;
  zoomMeetingUrl = '';
  isInitializingSdk = false;
  sdkReady = false;
  sdkError = '';
  isLoadingMeeting = false;
  participantSignature = '';
  private hasRetriedAsParticipant = false;
  private zoomClient: any = null;

  currentUser: UserDto | null = null;
  ProfileType = ProfileType;
  ChatType = ChatType;
  Array = Array;
  selectedChat: ChatDetailDto | null = null;
  isChatLoading = false;
  chatError = '';
  messageContent = '';
  isSendingMessage = false;
  typingUsers: Set<string> = new Set();
  isSignalRConnected = false;
  showRatingModal = false;
  ratingScore: number | null = null;
  ratingComment = '';
  ratingError = '';
  isSubmittingRating = false;
  isLoadingRating = false;
  isEditingRating = false;
  existingRatingId: string | null = null;
  private chatInitialized = false;
  private meetingTeacherId = '';
  private meetingStudentId = '';
  private typingTimeout: any;
  private shouldScrollToBottom = false;

  zoomSdkConfig = {
    sdkKey: '',
    signature: '',
    meetingNumber: '',
    userName: this.generateUniqueUsername(),
    userEmail: '',
    passWord: '',
    role: 0
  };

  constructor(
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  private generateUniqueUsername(): string {
    const randomId = Math.floor(Math.random() * 100000);
    const timestamp = Date.now().toString().slice(-6);
    return `Participant-${randomId}-${timestamp}`;
  }


  async ngOnInit(): Promise<void> {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.chatSignalRService.connect()
          .then(() => {
            this.isSignalRConnected = true;
          })
          .catch(err => {
            console.error('Failed to connect to SignalR:', err);
            this.isSignalRConnected = false;
          })
          .finally(() => this.tryInitChat());
      } else {
        this.isSignalRConnected = false;
      }
    });

    this.chatSignalRService.messageReceived$.subscribe(message => {
      if (!message || !this.selectedChat) return;
      if (message.chatId !== this.selectedChat.chatId) return;
      const exists = this.selectedChat.messages.some(m => m.messageId === message.messageId);
      if (!exists) {
        this.selectedChat.messages.push(message);
        this.scrollMessagesToBottom();
      }
    });

    this.chatSignalRService.userTyping$.subscribe(userName => {
      if (userName) {
        this.typingUsers.add(userName);
      }
    });

    this.chatSignalRService.userStoppedTyping$.subscribe(() => {
      this.typingUsers.clear();
    });

    this.chatSignalRService.connectionStatus$.subscribe(status => {
      this.isSignalRConnected = status;
      if (status && this.selectedChat) {
        this.chatSignalRService.joinChat(this.selectedChat.chatId).catch(err => {
          console.error('Error rejoining chat:', err);
        });
      }
    });

    // Get meeting ID from route parameter
    this.route.params.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.resetChatState();
        this.meetingId = parseInt(id, 10);
        this.courseId = null; // Will be set from meeting response
        // Wait for view to be ready before loading meeting
        if (this.viewReady) {
          this.loadMeetingAndInit();
        }
      } else {
        this.sdkError = 'ID de reunion manquant';
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.cdr.detectChanges();
    
    // Use requestAnimationFrame + setTimeout to ensure DOM is fully rendered
    requestAnimationFrame(() => {
      setTimeout(() => {
        if (this.meetingId && !this.zoomSdkConfig.meetingNumber && !this.isLoadingMeeting) {
          this.loadMeetingAndInit();
        } else if (this.zoomSdkConfig.meetingNumber && !this.sdkReady && !this.isInitializingSdk) {
          this.tryInitZoomSdk();
        }
      }, 50);
    });
  }

  ngAfterViewChecked(): void {
    if (!this.shouldScrollToBottom || !this.chatMessages?.nativeElement) return;
    const container = this.chatMessages.nativeElement;
    container.scrollTop = container.scrollHeight;
    this.shouldScrollToBottom = false;
  }

  ngOnDestroy(): void {
    // Clean up Zoom client when component is destroyed
    if (this.zoomClient) {
      try {
        this.zoomClient.leaveMeeting?.();
        this.zoomClient = null;
      } catch (err) {
        console.error('Error cleaning up Zoom client:', err);
      }
    }

    if (this.selectedChat) {
      this.chatSignalRService.leaveChat(this.selectedChat.chatId).catch(err => {
        console.error('Error leaving chat:', err);
      });
    }

    this.chatSignalRService.disconnect().catch(err => {
      console.error('Error disconnecting from SignalR:', err);
    });

    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }
  }

  openZoomInNewTab() {
    if (this.zoomMeetingUrl && typeof window !== 'undefined') {
      window.open(this.zoomMeetingUrl, '_blank', 'noopener,noreferrer');
    }
  }

  async leaveVisio(): Promise<void> {
    if (this.zoomClient) {
      try {
        this.zoomClient.leaveMeeting?.();
      } catch (err) {
        console.error('Error leaving Zoom meeting:', err);
      } finally {
        this.zoomClient = null;
      }
    }

    this.sdkReady = false;
    this.isInitializingSdk = false;
    this.sdkError = '';

    if (this.zoomContainer?.nativeElement) {
      this.zoomContainer.nativeElement.innerHTML = '';
    }

    this.cdr.detectChanges();

    // If teacher is leaving, mark the course as completed
    if (this.currentUser?.profileType === ProfileType.Teacher && this.courseId) {
      await this.markCourseAsCompleted();
    }

    if (this.currentUser?.profileType === ProfileType.Parent) {
      this.openRatingModal();
      return;
    }

    this.navigateHome();
  }

  private async markCourseAsCompleted(): Promise<void> {
    if (!this.courseId) return;

    try {
      await firstValueFrom(
        this.http.put(`${this.coursesBaseUrl}/${this.courseId}`, {
          status: 'COMPLETED'
        })
      );
    } catch (err) {
      console.error('Error marking course as completed:', err);
    }
  }

  async loadMeetingAndInit(): Promise<void> {
    if (!this.meetingId) {
      this.sdkError = 'ID de reunion invalide';
      return;
    }

    this.isLoadingMeeting = true;
    this.sdkError = '';

    try {
      const data = await firstValueFrom(
        this.http.get<MeetingDetailsResponse>(`${this.apiBaseUrl}/meetings/${this.meetingId}`)
      );

      // Update config with meeting data
      this.zoomSdkConfig.meetingNumber = data.meetingNumber;
      this.zoomSdkConfig.passWord = data.password || '';
      this.participantSignature = data.participantSignature || '';
      this.zoomSdkConfig.signature = this.participantSignature;
      this.zoomSdkConfig.sdkKey = data.sdkKey;
      this.zoomMeetingUrl = data.joinUrl;
      this.hasRetriedAsParticipant = false;
      this.meetingTeacherId = data.teacherId;
      this.meetingStudentId = data.studentId;
      
      // Extract courseId from response
      if (data.courseId) {
        this.courseId = data.courseId;
      }

      this.cdr.detectChanges();

      // Auto-initialize SDK with a small delay to ensure DOM is stable
      if (this.viewReady) {
        setTimeout(() => this.tryInitZoomSdk(), 100);
      }

      this.tryInitChat();
    } catch (err) {
      this.sdkError = this.getErrorMessage(err, 'Erreur lors du chargement de la réunion');
      console.error('Meeting loading error:', err);
    } finally {
      this.isLoadingMeeting = false;
    }
  }

  async createAndInitMeeting(): Promise<void> {
    this.isLoadingMeeting = true;
    this.sdkError = '';

    try {
      const data = await firstValueFrom(
        this.http.post<CreateMeetingResponse>(`${this.apiBaseUrl}/meeting`, {
          topic: 'ApprendsMoi - Session de classe',
          teacherId: 2,
          studentId: 3
        })
      );

      // Update config with meeting data
      this.zoomSdkConfig.meetingNumber = data.meetingNumber;
      this.zoomSdkConfig.passWord = data.password || '';
      this.participantSignature = data.participantSignature || '';
      this.zoomSdkConfig.signature = this.participantSignature;
      this.zoomSdkConfig.sdkKey = data.sdkKey;
      this.zoomMeetingUrl = data.joinUrl;
      this.hasRetriedAsParticipant = false;
      this.meetingTeacherId = data.teacherId;
      this.meetingStudentId = data.studentId;

      this.cdr.detectChanges();

      // Auto-initialize SDK with a small delay to ensure DOM is stable
      if (this.viewReady) {
        setTimeout(() => this.tryInitZoomSdk(), 100);
      }

      this.tryInitChat();
    } catch (err) {
      this.sdkError = this.getErrorMessage(err, 'Erreur lors de la création de la réunion');
      console.error('Meeting creation error:', err);
    } finally {
      this.isLoadingMeeting = false;
    }
  }

  async initZoomSdk(): Promise<void> {
    if (!this.zoomSdkConfig.meetingNumber) {
      await this.createAndInitMeeting();
    } else {
      this.tryInitZoomSdk();
    }
  }

  private tryInitZoomSdk(): void {
    if (!this.zoomContainer?.nativeElement) {
      if (this.viewReady) {
        // Retry after a short delay to ensure DOM is ready
        setTimeout(() => {
          if (this.zoomContainer?.nativeElement && !this.sdkReady && !this.isInitializingSdk) {
            this.tryInitZoomSdk();
          } else if (!this.zoomContainer?.nativeElement) {
            this.sdkError = 'Container Zoom non trouvé';
          }
        }, 200);
      }
      return;
    }

    if (this.sdkReady || this.isInitializingSdk) return;

    if (typeof ZoomMtgEmbedded === 'undefined') {
      // SDK might not be loaded yet, retry
      setTimeout(() => {
        if (typeof ZoomMtgEmbedded !== 'undefined' && !this.sdkReady && !this.isInitializingSdk) {
          this.tryInitZoomSdk();
        } else if (typeof ZoomMtgEmbedded === 'undefined') {
          this.sdkError = 'SDK Zoom non chargé.';
        }
      }, 500);
      return;
    }

    this.isInitializingSdk = true;
    this.sdkError = '';
    this.cdr.detectChanges();

    // Create a new client instance for this session
    this.zoomClient = ZoomMtgEmbedded.createClient();
    const container = this.zoomContainer.nativeElement;

    // Clear the container before initializing
    container.innerHTML = '';

    this.zoomClient
      .init({
        zoomAppRoot: container,
        language: 'fr-FR',
        leaveUrl: window.location.origin,
        customize: {
          video: {
            isResizable: false,
            viewSizes: {
              default: {
                width: 700,
                height: 250
              }
            }
          }
        }
      })
      .then(() => this.zoomClient.join({
        sdkKey: this.zoomSdkConfig.sdkKey,
        signature: this.zoomSdkConfig.signature,
        meetingNumber: this.zoomSdkConfig.meetingNumber,
        password: this.zoomSdkConfig.passWord,
        userName: this.zoomSdkConfig.userName,
        ...(this.zoomSdkConfig.userEmail && { userEmail: this.zoomSdkConfig.userEmail })
      }))
      .then(() => {
        this.sdkReady = true;
        this.isInitializingSdk = false;
        this.cdr.detectChanges();
      })
      .catch((err: any) => {
        this.isInitializingSdk = false;
        const formattedError = this.formatZoomError(err);
        console.error('Zoom SDK error:', err);

        // Fallback: retry once as participant (never escalate to host)
        if (!this.hasRetriedAsParticipant && this.participantSignature) {
          this.hasRetriedAsParticipant = true;
          this.zoomSdkConfig.signature = this.participantSignature;
          this.zoomSdkConfig.role = 0;
          this.zoomSdkConfig.userName = this.generateUniqueUsername();
          this.tryInitZoomSdk();
          return;
        }

        this.sdkError = formattedError;
        this.cdr.detectChanges();
      });
  }

  private tryInitChat(): void {
    if (this.chatInitialized) return;
    if (!this.currentUser || !this.meetingTeacherId) return;

    this.chatInitialized = true;
    this.loadChatForMeeting();
  }

  private loadChatForMeeting(): void {
    this.isChatLoading = true;
    this.chatError = '';

    this.chatService.getChats().subscribe({
      next: (chats) => {
        const existing = this.findMatchingChat(chats);
        if (existing) {
          this.openChat(existing.chatId);
        } else {
          this.createChatForMeeting();
        }
      },
      error: (err) => {
        this.chatError = 'Impossible de charger le chat';
        this.toastService.error('Impossible de charger le chat');
        console.error(err);
        this.isChatLoading = false;
      }
    });
  }

  private findMatchingChat(chats: ChatDto[]): ChatDto | null {
    if (!chats.length) return null;

    const exact = chats.find(chat =>
      chat.teacherId === this.meetingTeacherId &&
      this.meetingStudentId &&
      chat.studentId === this.meetingStudentId
    );
    if (exact) return exact;

    if (this.currentUser?.profileType === ProfileType.Parent) {
      const parentChat = chats.find(chat =>
        chat.teacherId === this.meetingTeacherId &&
        chat.chatType === ChatType.ParentChat
      );
      if (parentChat) return parentChat;
    }

    if (this.currentUser?.profileType === ProfileType.Student || this.currentUser?.profileType === ProfileType.Teacher) {
      const studentChat = chats.find(chat =>
        chat.teacherId === this.meetingTeacherId &&
        chat.chatType === ChatType.StudentChat
      );
      if (studentChat) return studentChat;
    }

    return chats.find(chat => chat.teacherId === this.meetingTeacherId) || null;
  }

  private createChatForMeeting(): void {
    if (!this.meetingTeacherId || !this.currentUser) {
      this.chatError = 'Chat indisponible pour cette session.';
      this.isChatLoading = false;
      return;
    }

    const chatType = this.currentUser.profileType === ProfileType.Parent ? ChatType.ParentChat : ChatType.StudentChat;
    const dto: CreateChatDto = {
      chatType,
      teacherId: this.meetingTeacherId
    };
    if (chatType === ChatType.StudentChat && this.meetingStudentId) {
      dto.studentId = this.meetingStudentId;
    }

    this.chatService.createChat(dto).subscribe({
      next: (chat) => {
        this.openChat(chat.chatId);
      },
      error: (err) => {
        this.chatError = 'Impossible de créer le chat';
        this.toastService.error('Impossible de creer le chat');
        console.error(err);
        this.isChatLoading = false;
      }
    });
  }

  private openChat(chatId: string): void {
    this.chatService.getChatDetail(chatId).subscribe({
      next: (chatDetail) => {
        this.selectedChat = {
          ...chatDetail,
          messages: chatDetail.messages || []
        };
        this.messageContent = '';
        this.typingUsers.clear();
        this.isChatLoading = false;

        this.chatService.markChatAsRead(chatId).subscribe({
          error: (err) => console.error('Failed to mark chat as read', err)
        });

        this.chatSignalRService.joinChat(chatId).catch(err => {
          console.error('Error joining chat:', err);
        });

        this.scrollMessagesToBottom();
      },
      error: (err) => {
        this.chatError = 'Impossible de charger le chat';
        this.toastService.error('Impossible de charger le chat');
        console.error(err);
        this.isChatLoading = false;
      }
    });
  }

  sendMessage(): void {
    if (!this.selectedChat || !this.messageContent.trim()) {
      return;
    }

    this.isSendingMessage = true;
    const content = this.messageContent.trim();

    this.chatService.sendMessage(this.selectedChat.chatId, { Content: content }).subscribe({
      next: (message) => {
        if (this.selectedChat) {
          const exists = this.selectedChat.messages.some(m => m.messageId === message.messageId);
          if (!exists) {
            this.selectedChat.messages.push(message);
          }

          this.messageContent = '';
          this.isSendingMessage = false;
          this.typingUsers.clear();
          this.scrollMessagesToBottom();

          this.chatSignalRService.sendMessageToChat(this.selectedChat.chatId, message).catch(err => {
            console.error('Error sending message via SignalR:', err);
          });

          if (this.currentUser) {
            this.chatSignalRService.notifyStoppedTyping(this.selectedChat.chatId).catch(err => {
              console.error('Error notifying stopped typing:', err);
            });
          }
        }
      },
      error: (err) => {
        this.chatError = 'Impossible d\'envoyer le message';
        this.toastService.error('Impossible d\'envoyer le message');
        console.error(err);
        this.isSendingMessage = false;
      }
    });
  }

  onMessageInput(): void {
    if (!this.selectedChat || !this.currentUser) {
      return;
    }

    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }

    this.chatSignalRService.notifyTyping(this.selectedChat.chatId, this.currentUser.firstName || 'Utilisateur').catch(err => {
      console.error('Error notifying typing:', err);
    });

    this.typingTimeout = setTimeout(() => {
      this.chatSignalRService.notifyStoppedTyping(this.selectedChat!.chatId).catch(err => {
        console.error('Error notifying stopped typing:', err);
      });
    }, 2000);
  }

  private scrollMessagesToBottom(): void {
    this.shouldScrollToBottom = true;
  }

  private resetChatState(): void {
    if (this.selectedChat) {
      this.chatSignalRService.leaveChat(this.selectedChat.chatId).catch(err => {
        console.error('Error leaving chat:', err);
      });
    }

    this.selectedChat = null;
    this.messageContent = '';
    this.chatError = '';
    this.isChatLoading = false;
    this.isSendingMessage = false;
    this.typingUsers.clear();
    this.chatInitialized = false;
    this.meetingTeacherId = '';
    this.meetingStudentId = '';
  }

  private openRatingModal(): void {
    if (!this.meetingTeacherId) {
      this.toastService.warning('Aucun professeur a noter pour cette session.');
      return;
    }

    this.showRatingModal = true;
    this.ratingScore = null;
    this.ratingComment = '';
    this.ratingError = '';
    this.isSubmittingRating = false;
    this.isLoadingRating = true;
    this.isEditingRating = false;
    this.existingRatingId = null;

    const parentId = this.currentUser?.id;
    if (!parentId) {
      this.isLoadingRating = false;
      return;
    }

    this.http.get<RatingDto[]>(`${environment.apiUrl}/api/Ratings/parent/${parentId}`).subscribe({
      next: (ratings) => {
        const matching = ratings
          .filter(r => r.teacherId === this.meetingTeacherId)
          .sort((a, b) => {
            const aTime = new Date(a.updatedAt || a.createdAt || 0).getTime();
            const bTime = new Date(b.updatedAt || b.createdAt || 0).getTime();
            return bTime - aTime;
          });

        const existing = matching[0];
        if (existing) {
          this.isEditingRating = true;
          this.existingRatingId = existing.id;
          this.ratingScore = existing.rating ?? null;
          this.ratingComment = existing.comment ?? '';
        }
        this.isLoadingRating = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoadingRating = false;
      }
    });
  }

  closeRatingModal(): void {
    this.showRatingModal = false;
    this.ratingError = '';
    this.isLoadingRating = false;
    this.isEditingRating = false;
    this.existingRatingId = null;
    this.navigateHome();
  }

  setRating(score: number): void {
    this.ratingScore = score;
    this.ratingError = '';
  }

  submitRating(): void {
    if (!this.meetingTeacherId) {
      this.ratingError = 'Professeur introuvable pour cette session.';
      return;
    }

    if (!this.ratingScore) {
      this.ratingError = 'Veuillez choisir une note.';
      return;
    }

    this.isSubmittingRating = true;
    const payload = {
      rating: this.ratingScore,
      comment: this.ratingComment?.trim() || null
    };

    const request$ = this.isEditingRating && this.existingRatingId
      ? this.http.put(`${environment.apiUrl}/api/Ratings/${this.existingRatingId}`, payload)
      : this.http.post(`${environment.apiUrl}/api/Ratings`, {
          teacherId: this.meetingTeacherId,
          ...payload
        });

    request$.subscribe({
      next: () => {
        this.isSubmittingRating = false;
        this.showRatingModal = false;
        this.toastService.success(this.isEditingRating ? 'Note mise a jour.' : 'Merci pour votre note.');
        this.navigateHome();
      },
      error: (err) => {
        console.error(err);
        this.isSubmittingRating = false;
        this.ratingError = "Impossible d'envoyer votre note.";
        this.toastService.error("Impossible d'envoyer votre note.");
      }
    });
  }

  private navigateHome(): void {
    this.router.navigate(['/']);
  }

  private getErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      if (typeof err.error === 'string') return err.error;
      if (err.error?.error) return err.error.error;
      return err.message || fallback;
    }

    if (err instanceof Error) return err.message;
    return fallback;
  }

  private formatZoomError(err: any): string {
    let msg = 'Défaut de se joindre a la reunion.';
    if (err?.reason) msg += ` ${err.reason}`;
    if (err?.errorCode) msg += ` (Code: ${err.errorCode})`;
    return msg;
  }
}
