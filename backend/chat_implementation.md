# Chat Implementation Plan

## Overview
Implement a multi-channel messaging system for ApprendsMoi that allows:
- Teachers to chat with Parents (separate conversations)
- Teachers to chat with Students (separate conversations)
- File sharing system (accessible from chats or as a separate shared files section)

---

## 1. Database Schema & Models

### 1.1 New Models to Create

#### Chat Model
```
Chat
├── ChatId (PK, Guid)
├── ChatType (enum: ParentChat, StudentChat)
├── TeacherId (FK to Teacher)
├── ParentId (FK to Parent, nullable)
├── StudentId (FK to Student, nullable)
├── CreatedAt (DateTime)
├── UpdatedAt (DateTime)
├── IsActive (bool)
└── Messages (collection)
```

#### Message Model
```
Message
├── MessageId (PK, Guid)
├── ChatId (FK to Chat)
├── SenderId (FK to User)
├── Content (string)
├── CreatedAt (DateTime)
└── Attachments (collection)
```

#### ChatAttachment/FileShare Model
```
ChatAttachment
├── AttachmentId (PK, Guid)
├── MessageId (FK to Message, nullable)
├── ChatId (FK to Chat, nullable)
├── FileName (string)
├── FileUrl (string)
├── FileSize (long)
├── FileType (string)
├── UploadedBy (FK to User)
└── CreatedAt (DateTime)
```

### 1.2 Enums
- `ChatType`: ParentChat, StudentChat

### 1.3 Update User Model
- Add navigation properties: `SentMessages`, `Chats` (if participating)

---

## 2. Database Migrations

### Tasks
- [ ] Create migration: `AddChatAndMessagesTable`
  - Create Chat table
  - Create Message table
  - Create ChatAttachment table
  - Create ChatParticipant table (optional)
  - Add foreign keys
  - Add indexes on ChatId, SenderId, CreatedAt
- [ ] Apply migrations to development database
- [ ] Update `AppDbContext.cs` with DbSets

---

## 3. Data Transfer Objects (DTOs)

### Location: `Domains/Chat/`

#### ChatDto
```
{
  chatId: string (Guid)
  chatType: string (ParentChat | StudentChat)
  teacherId: string
  parentId: string (nullable)
  studentId: string (nullable)
  participantName: string (Parent or Student name)
  lastMessage: string (preview)
  lastMessageTime: DateTime
  unreadCount: int
}
```

#### MessageDto
```
{
  messageId: string
  chatId: string
  senderId: string
  senderName: string
  content: string
  createdAt: DateTime
  attachments: ChatAttachmentDto[]
}
```

#### CreateMessageDto
```
{
  content: string (required, max 5000 chars)
  attachments: IFormFile[] (optional)
}
```

#### ChatAttachmentDto
```
{
  attachmentId: string
  fileName: string
  fileUrl: string
  fileSize: long
  fileType: string
  uploadedBy: string
  uploadedByName: string
  createdAt: DateTime
}
```

#### CreateChatDto
```
{
  chatType: string (ParentChat | StudentChat)
  teacherId: string
  parentId: string (if ParentChat)
  studentId: string (if StudentChat)
}
```

---

## 4. Services Layer

### Location: `Domains/Chat/Services/`

#### ChatService
- `GetChatsByTeacher(teacherId)` → List<ChatDto>
- `GetChatById(chatId)` → ChatDto (with messages)
- `CreateChat(createChatDto, teacherId)` → ChatDto
- `GetOrCreateChat(otherUserId, chatType, teacherId)` → ChatDto
- `ArchiveChat(chatId)` → bool
- `GetUnreadChatsCount(userId)` → int

#### MessageService
- `GetMessagesByChat(chatId, pageNumber, pageSize)` → List<MessageDto> (paginated)
- `CreateMessage(messageDto, senderId, chatId)` → MessageDto
- `MarkChatAsRead(chatId, userId)` → bool
- `SearchMessages(chatId, searchTerm)` → List<MessageDto>

#### ChatAttachmentService
- `UploadAttachment(file, uploadedBy)` → ChatAttachmentDto
- `GetAttachmentsByChat(chatId)` → List<ChatAttachmentDto>
- `DeleteAttachment(attachmentId, userId)` → bool
- `GetSharedFilesByTeacher(teacherId)` → List<ChatAttachmentDto> (all shared files)

---

## 5. Controllers

### Location: `Domains/Chat/Controllers/`

#### ChatController
```
[Route("api/chats")]
[Authorize]
```

**Endpoints:**
- `GET /api/chats` - Get all chats for logged-in user (teacher/parent/student)
- `GET /api/chats/{chatId}` - Get specific chat with messages
- `POST /api/chats` - Create new chat
- `GET /api/chats/{chatId}/archive` - Archive a chat
- `GET /api/chats/unread-count` - Get unread count

#### MessageController
```
[Route("api/messages")]
[Authorize]
```

**Endpoints:**
- `GET /api/messages/{chatId}?page=1&pageSize=20` - Get messages (paginated)
- `POST /api/messages/{chatId}` - Send message (with optional file uploads)
- `POST /api/messages/{chatId}/mark-read` - Mark chat as read
- `GET /api/messages/search/{chatId}?term=xxx` - Search messages

#### FileShareController
```
[Route("api/files")]
[Authorize]
```

**Endpoints:**
- `GET /api/files/teacher/{teacherId}` - Get all shared files by a teacher
- `GET /api/files/{chatId}` - Get files shared in a specific chat
- `DELETE /api/files/{attachmentId}` - Delete a shared file

---

## 6. Authorization & Security

### Rules
- [ ] Parents can initiate chats with any teacher
- [ ] Students can chat with their teachers
- [ ] Messages are immutable (cannot be edited or deleted after creation)
- [ ] Only message sender can delete their own attachments
- [ ] Only file uploader can delete attachments
- [ ] Implement authorization attributes: `[Authorize(Roles = "Teacher")]`, etc.
- [ ] File uploads: Validate file types and size (max 50MB per file)
- [ ] Create custom `[AuthorizeChat]` attribute to verify user access to specific chat

### File Upload Security
- Store files in `/uploads/chats/{chatId}/` or cloud storage (Azure Blob, S3)
- Sanitize file names
- Validate MIME types
- Implement virus scanning for uploaded files (optional)
- Enforce maximum file size limits

---

## 7. Real-Time Communication (SignalR)

### Core Implementation
- [ ] Install NuGet package: `Microsoft.AspNetCore.SignalR`
- [ ] Create `ChatHub` class for real-time messaging
- [ ] Implement connections per user
- [ ] Send messages in real-time instead of polling
- [ ] Handle user connect/disconnect
- [ ] Map SignalR hub in Program.cs: `app.MapHub<ChatHub>("/hubs/chat")`

### ChatHub Methods
```csharp
public async Task SendMessage(string chatId, string content, List<IFormFile> attachments)
public async Task ReceiveMessage(MessageDto message)
public async Task UserOnline(string userId)
public async Task UserOffline(string userId)
```

---

## 8. File Storage

### Options
1. **Local File System** (Simple, suitable for development)
   - Store in `wwwroot/uploads/chats/`
   - Serve via static files middleware
   - Location: `FileUploadService`

2. **Azure Blob Storage** (Production recommended)
   - Install: `Azure.Storage.Blobs`
   - Configure connection string in `appsettings.json`
   - Generate SAS URLs for secure access
   - Location: `AzureBlobService`

### Implementation
- [ ] Create `IFileStorageService` interface
- [ ] Implement `LocalFileStorageService` for development
- [ ] Implement `AzureBlobStorageService` for production
- [ ] Dependency injection based on environment
- [ ] Handle file cleanup (soft delete or scheduled deletion)

---

## 9. Database Indexing & Performance

### Indexes to Add
```sql
CREATE INDEX idx_chat_teacherid ON chats(teacher_id)
CREATE INDEX idx_chat_parentid ON chats(parent_id)
CREATE INDEX idx_chat_studentid ON chats(student_id)
CREATE INDEX idx_message_chatid ON messages(chat_id)
CREATE INDEX idx_message_senderid ON messages(sender_id)
CREATE INDEX idx_message_createdat ON messages(created_at DESC)
CREATE INDEX idx_attachment_chatid ON chat_attachments(chat_id)
CREATE INDEX idx_attachment_messageid ON chat_attachments(message_id)
```

### Query Optimization
- [ ] Use `.Include()` to avoid N+1 queries
- [ ] Implement pagination for large message lists
- [ ] Cache recent chats (optional)
- [ ] Lazy load messages on demand

---

## 10. Testing

### Unit Tests
- [ ] ChatService tests
- [ ] MessageService tests
- [ ] Authorization tests
- [ ] File upload validation tests

### Integration Tests
- [ ] Chat creation flow
- [ ] Message sending flow
- [ ] File upload and retrieval
- [ ] Permission checks

### Location: `backend.Tests/Domains/Chat/`

---

## 11. Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- [ ] Database models and migrations
- [ ] DTOs
- [ ] Services (ChatService, MessageService)
- [ ] Controllers with basic endpoints
- [ ] Authorization attributes

### Phase 2: File Handling (Week 1-2)
- [ ] File upload service (local storage)
- [ ] Attachment model and service
- [ ] File endpoints
- [ ] File security validation

### Phase 3: Frontend Basic Chat (Week 2)
- [ ] Chat list component
- [ ] Chat window component
- [ ] Message display and input
- [ ] Basic styling and UX
- [ ] SignalR integration for real-time updates

### Phase 4: Advanced Features (Week 3+)
- [ ] Message search optimization
- [ ] Shared files dedicated view
- [ ] Performance monitoring

### Phase 5: Production Readiness (Week 4+)
- [ ] Migrate to Azure Blob Storage
- [ ] Comprehensive testing
- [ ] Performance optimization
- [ ] Security audit
- [ ] Error handling & logging

---

## 12. Environment Configuration

### .env File
```
# File Storage
FILE_STORAGE_PROVIDER=Local
FILE_STORAGE_LOCAL_PATH=uploads/chats
FILE_STORAGE_MAX_SIZE_MB=50
FILE_STORAGE_ALLOWED_EXTENSIONS=.pdf,.doc,.docx,.jpg,.png,.xlsx

# Chat Configuration
CHAT_MAX_MESSAGE_LENGTH=5000
CHAT_MESSAGE_HISTORY_PAGE_SIZE=20

# SignalR
SIGNALR_HUB_URL=/hubs/chat
```

### Environment Variable Defaults
- FILE_STORAGE_PROVIDER: "Local"
- FILE_STORAGE_LOCAL_PATH: "uploads/chats"
- FILE_STORAGE_MAX_SIZE_MB: 50
- CHAT_MAX_MESSAGE_LENGTH: 5000
- CHAT_MESSAGE_HISTORY_PAGE_SIZE: 20

---

## 13. API Documentation

### Swagger/OpenAPI Integration
- [ ] Configure Swagger for chat endpoints in Program.cs
- [ ] Document all DTOs with XML comments
- [ ] Include authentication examples (Bearer token)
- [ ] Add error response examples (400, 401, 403, 404, 500)
- [ ] Document SignalR hub in Swagger comments
- [ ] Include file upload endpoint documentation with multipart/form-data examples

---

## 14. Quick Reference: File Structure

```
backend/
├── Database/
│   ├── Models/
│   │   ├── Chat.cs (NEW)
│   │   ├── Message.cs (NEW)
│   │   └── ChatAttachment.cs (NEW)
│   ├── Migrations/
│   │   └── AddChatAndMessagesTable.cs (NEW)
│   └── AppDbContext.cs (UPDATE)
├── Domains/
│   └── Chat/ (NEW)
│       ├── ChatDto.cs
│       ├── MessageDto.cs
│       ├── ChatAttachmentDto.cs
│       ├── Controllers/
│       │   ├── ChatController.cs
│       │   ├── MessageController.cs
│       │   └── FileShareController.cs
│       ├── Services/
│       │   ├── ChatService.cs
│       │   ├── MessageService.cs
│       │   ├── ChatAttachmentService.cs
│       │   └── IFileStorageService.cs
│       └── Hubs/
│           └── ChatHub.cs (NEW - SignalR)
├── Helpers/
│   ├── AuthorizeChatAttribute.cs (NEW)
│   └── FileValidationHelper.cs (NEW)
└── Program.cs (UPDATE: Register services, SignalR, Swagger)
```

---

## 15. Status Checklist

- [ ] Database models created
- [ ] Migrations applied
- [ ] DTOs implemented
- [ ] Services implemented
- [ ] Controllers implemented
- [ ] Authorization implemented
- [ ] File upload service implemented
- [ ] SignalR ChatHub implemented
- [ ] Swagger documentation configured
- [ ] Testing completed
- [ ] Production deployment ready
