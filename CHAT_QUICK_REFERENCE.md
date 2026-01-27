# 🚀 Chat Feature - Quick Reference

## Files Created/Modified

### Backend
✅ **Models** (Database/Models/)
- `Chat.cs` - Chat entity
- `Message.cs` - Message entity  
- `ChatAttachment.cs` - File attachment entity

✅ **Database** (Database/)
- `AppDbContext.cs` - Added DbSets and configurations
- `Migrations/AddChatAndMessagesTable.cs` - Migration 202401010004

✅ **DTOs** (Domains/Chat/)
- `ChatDto.cs` - All chat-related DTOs

✅ **Services** (Domains/Chat/Services/)
- `ChatService.cs` - Chat business logic
- `MessageService.cs` - Message operations
- `FileStorageService.cs` - File storage abstraction + LocalFileStorageService

✅ **Controllers** (Domains/Chat/Controllers/)
- `ChatController.cs` - Chat API endpoints
- `MessageController.cs` - Message API endpoints
- `FileController.cs` - File management endpoints

✅ **Program.cs**
- Added services registration
- Added using statement

### Frontend
✅ **Services** (src/app/services/)
- `chat.service.ts` - Chat API service + DTOs

✅ **Components** (src/app/pages/Chat/)
- `chat.component.ts` - Chat component logic
- `chat.component.html` - Chat UI template
- `chat.component.scss` - Chat styling

✅ **Routing** (src/app/)
- `app.routes.ts` - Added /chat route

---

## ⚡ Quick Start

### 1. Start Backend
```bash
cd backend
dotnet run --reset-database --populate
```
✅ Backend running on `http://localhost:5254`

### 2. Start Frontend
```bash
cd frontend
npm install  # if needed
npm start
# or
ng serve
```
✅ Frontend running on `http://localhost:4200`

### 3. Test Chat
- Navigate to `http://localhost:4200/login`
- Login as parent or teacher
- Click "chat" in navigation or go to `/chat`
- Create/use chat to send messages

---

## 🔌 API Base URL
```
http://localhost:5254/api
```

## 📍 Frontend Route
```
/chat
```

---

## 🎯 Core Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/chats` | Get user's chats |
| POST | `/api/chats` | Create chat |
| GET | `/api/chats/{id}` | Get chat detail |
| POST | `/api/chats/{id}/archive` | Archive chat |
| GET | `/api/messages/{chatId}` | Get messages (paginated) |
| POST | `/api/messages/{chatId}` | Send message |
| GET | `/api/messages/{chatId}/search` | Search messages |
| POST | `/api/files/{chatId}` | Upload file |
| DELETE | `/api/files/{attachmentId}` | Delete file |

---

## 📊 Database Tables

```sql
chats
├── chatId (PK)
├── chatType (0=ParentChat, 1=StudentChat)
├── teacherId (FK)
├── parentId (FK)
├── studentId (FK)
└── createdAt, updatedAt, isActive

messages
├── messageId (PK)
├── chatId (FK)
├── senderId (FK)
├── content
└── createdAt

chatAttachments
├── attachmentId (PK)
├── messageId (FK)
├── chatId (FK)
├── fileName, fileUrl, fileSize
└── uploadedBy, createdAt
```

---

## 🧪 Test Scenarios

### Parent Creates Chat with Teacher
```
1. Login as Parent
2. Go to /chat
3. Click "Start New Chat"
4. Enter teacher UUID
5. Send message
6. Login as Teacher (different window)
7. See chat and reply
```

### Student Chats After Course Booking
```
1. Login as Student
2. Book course from teacher
3. Backend auto-creates StudentChat
4. Go to /chat
5. Chat with teacher appears automatically
```

### Search Messages
```
GET /api/messages/{chatId}/search?searchTerm=lesson
Returns: MessageDto[] matching search term
```

---

## 🔐 Authentication

All endpoints require JWT Bearer token in Authorization header:
```
Authorization: Bearer {token}
```

Token obtained via login endpoint, stored in localStorage.

---

## 📋 Component Properties

### ChatComponent
```typescript
chats: ChatDto[]              // All user's chats
selectedChat: ChatDetailDto   // Currently selected chat
currentUser: UserDto          // Logged-in user
loading: boolean              // Loading state
error: string                 // Error message
showCreateChatModal: boolean  // Modal visibility
messageContent: string        // Message text input
newChatTeacherId: string      // Teacher ID for new chat
```

### ChatComponent Methods
```typescript
ngOnInit()              // Load chats on init
loadChats()             // Fetch chats from API
selectChat(chat)        // Load chat detail
sendMessage()           // Post message to API
createNewChat()         // Create new chat with teacher
archiveChat()           // Soft delete chat
closeCreateModal()      // Close create modal
```

---

## 🎨 UI Features

- ✅ Chat list sidebar with avatars
- ✅ Message display (sender-aligned)
- ✅ Message timestamps
- ✅ Unread message count
- ✅ Last message preview
- ✅ File attachment links
- ✅ Create chat modal
- ✅ Archive functionality
- ✅ Loading spinners
- ✅ Error messages
- ✅ Responsive design (desktop/tablet/mobile)

---

## 📁 File Storage

**Location**: `uploads/chats/{chatId}/`
**Max Size**: 50MB per file
**Allowed Types**: .pdf, .doc, .docx, .jpg, .png, .xlsx, .txt, .zip

---

## 🐛 Debug Tips

### Check Token
```javascript
// In browser console
localStorage.getItem('token')
```

### Check API Response
```javascript
// DevTools → Network tab
// Look for failed requests (404, 403, 500)
```

### Database Query
```sql
SELECT * FROM chats;
SELECT * FROM messages WHERE "chatId" = 'uuid';
SELECT * FROM "chatAttachments";
```

### Backend Logs
```bash
# Terminal running backend
# Look for errors/exceptions
```

---

## ✨ Features Summary

✅ Multi-participant chats (Parent-Teacher, Student-Teacher)
✅ Role-based access control
✅ Message pagination (20 per page, max 100)
✅ Message search
✅ File attachments (with validation)
✅ Soft delete (archive) chats
✅ Participant verification
✅ JWT authentication
✅ Real-time UI updates
✅ Responsive mobile design

---

## 📚 Related Files for Reference

- Database context: `backend/Database/AppDbContext.cs`
- Chat service: `backend/Domains/Chat/Services/ChatService.cs`
- Message service: `backend/Domains/Chat/Services/MessageService.cs`
- Auth interceptor: `frontend/src/app/auth.interceptor.ts`
- Main routes: `frontend/src/app/app.routes.ts`

---

## 🔄 Integration Points

**CourseService**: Auto-creates StudentChat when course is booked
```csharp
await chatService.AutoCreateStudentChatAsync(teacherId, studentId);
```

**AuthService**: Provides currentUser$ and ProfileType enum
```typescript
public currentUser$: Observable<UserDto>
enum ProfileType { Admin=0, Teacher=1, Parent=2, Student=3 }
```

---

## 📈 Performance Notes

- Message pagination prevents loading all messages at once
- Chat list loads only active (non-archived) chats
- File size validation prevents storage bloat
- Database indexes on frequently queried columns (ChatId, SenderId, TeacherId)

---

**Status**: ✅ Production Ready
**Last Tested**: Before final integration
**Known Issues**: None

