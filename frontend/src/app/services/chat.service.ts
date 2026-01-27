import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export enum ChatType {
  ParentChat = 0,
  StudentChat = 1
}

export interface ChatDto {
  chatId: string;
  chatType: ChatType;
  teacherId: string;
  parentId?: string;
  studentId?: string;
  participantName: string;
  participantProfilePicture?: string;
  lastMessage?: string;
  lastMessageTime?: string;
  unreadCount: number;
  createdAt: string;
  updatedAt: string;
  isActive: boolean;
}

export interface ChatDetailDto extends ChatDto {
  messages: MessageDto[];
}

export interface MessageDto {
  messageId: string;
  chatId: string;
  senderId: string;
  senderName: string;
  senderProfilePicture?: string;
  content: string;
  createdAt: string;
  attachments: ChatAttachmentDto[];
}

export interface CreateMessageDto {
  Content: string;
}

export interface ChatAttachmentDto {
  attachmentId: string;
  fileName: string;
  fileUrl: string;
  fileSize: number;
  fileType: string;
  uploadedBy: string;
  uploadedByName: string;
  createdAt: string;
}

export interface CreateChatDto {
  chatType: ChatType;
  teacherId?: string;
  parentId?: string;
  studentId?: string;
}

export interface TeacherDto {
  id: string;
  email?: string;
  username?: string;
  firstName: string;
  lastName: string;
  profilePicture?: string;
  profile: number;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  bio?: string;
  phoneNumber?: string;
  verificationStatus?: number;
  isPremium?: boolean;
  city?: string;
  travelRadiusKm?: number;
}

export interface PaginatedMessagesDto {
  messages: MessageDto[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  // Get all chats for current user
  getChats(): Observable<ChatDto[]> {
    return this.http.get<ChatDto[]>(`${this.apiUrl}/chats`);
  }

  // Get specific chat with messages
  getChatDetail(chatId: string): Observable<ChatDetailDto> {
    return this.http.get<ChatDetailDto>(`${this.apiUrl}/chats/${chatId}`);
  }

  // Create a new chat
  createChat(dto: CreateChatDto): Observable<ChatDto> {
    return this.http.post<ChatDto>(`${this.apiUrl}/chats`, dto);
  }

  // Archive a chat
  archiveChat(chatId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/chats/${chatId}/archive`, {});
  }

  // Mark chat as read
  markChatAsRead(chatId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/chats/${chatId}/read`, {});
  }

  // Get messages with pagination
  getMessages(chatId: string, pageNumber: number = 1, pageSize: number = 20): Observable<PaginatedMessagesDto> {
    return this.http.get<PaginatedMessagesDto>(
      `${this.apiUrl}/messages/${chatId}?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
  }

  // Send a message
  sendMessage(chatId: string, dto: CreateMessageDto): Observable<MessageDto> {
    const formData = new FormData();
    formData.append('Content', dto.Content);
    return this.http.post<MessageDto>(`${this.apiUrl}/messages/${chatId}`, formData);
  }

  // Get recent messages
  getRecentMessages(chatId: string, limit: number = 50): Observable<MessageDto[]> {
    return this.http.get<MessageDto[]>(
      `${this.apiUrl}/messages/${chatId}/recent?limit=${limit}`
    );
  }

  // Search messages
  searchMessages(chatId: string, searchTerm: string): Observable<MessageDto[]> {
    return this.http.get<MessageDto[]>(
      `${this.apiUrl}/messages/${chatId}/search?searchTerm=${encodeURIComponent(searchTerm)}`
    );
  }

  // Search or list teachers (backend supports optional filters)
  searchTeachers(searchTerm: string = ''): Observable<TeacherDto[]> {
    const params: Record<string, string> = {};
    const trimmed = searchTerm.trim();
    if (trimmed) {
      // Backend currently supports optional filters; unused params are ignored
      params['search'] = trimmed;
    }
    return this.http.get<TeacherDto[]>(`${this.apiUrl}/Users/teachers`, { params });
  }
}
