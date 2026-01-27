# Chat API Quick Reference

## Base URL
```
http://localhost:5000/api
```

## Authentication
All endpoints require Bearer token in Authorization header:
```
Authorization: Bearer <JWT_TOKEN>
```

---

## Chat Endpoints

### Get All Chats
```
GET /chats
```
**Returns:** List of ChatDto
- Teacher: Gets all their chats with parents/students
- Parent: Gets all chats with teachers
- Student: Gets all chats with their teachers

### Get Chat Detail (with Messages)
```
GET /chats/{chatId}
```
**Returns:** ChatDetailDto (includes all messages)
**Authorization:** Must be participant in chat

### Create Chat
```
POST /chats
Content-Type: application/json

{
  "chatType": "ParentChat" | "StudentChat",
  "teacherId": "guid-here",
  "parentId": "guid-here", // for ParentChat
  "studentId": "guid-here"  // for StudentChat
}
```
**Returns:** ChatDto
**Authorization:** Teachers only
**Note:** Returns existing chat if already exists

### Archive Chat
```
POST /chats/{chatId}/archive
```
**Returns:** 204 No Content
**Authorization:** Must be participant

### Reactivate Chat
```
POST /chats/{chatId}/reactivate
```
**Returns:** 204 No Content
**Authorization:** Must be participant

---

## Message Endpoints

### Get Messages (Paginated)
```
GET /messages/{chatId}?pageNumber=1&pageSize=20
```
**Returns:** PaginatedMessagesDto
```json
{
  "messages": [
    {
      "messageId": "guid",
      "chatId": "guid",
      "senderId": "guid",
      "senderName": "John Doe",
      "senderProfilePicture": "url",
      "content": "Message text",
      "createdAt": "2026-01-27T10:30:00Z",
      "attachments": []
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 50,
  "totalPages": 3
}
```
**Authorization:** Must be participant
**Default:** 20 messages per page, max 100

### Send Message (with Optional Files)
```
POST /messages/{chatId}
Content-Type: multipart/form-data

Form Data:
- content: "Hello!" (required, max 5000 chars)
- attachments: [file1, file2, ...] (optional, up to file size limit)
```
**Returns:** MessageDto with attachments
**Authorization:** Must be participant
**File Limits:** 50MB per file (configurable), allowed: .pdf, .doc, .docx, .jpg, .png, .xlsx, .txt, .zip

### Search Messages
```
GET /messages/{chatId}/search?searchTerm=keyword
```
**Returns:** List<MessageDto> (all matching messages)
**Authorization:** Must be participant

### Get Recent Messages (for Sync)
```
GET /messages/{chatId}/recent?limit=50
```
**Returns:** List<MessageDto> (last N messages, newest last)
**Authorization:** Must be participant

---

## File Endpoints

### Upload File to Chat
```
POST /files/{chatId}
Content-Type: multipart/form-data

Form Data:
- file: [file] (required)
```
**Returns:** ChatAttachmentDto
**Authorization:** Must be participant in chat
**Use:** For sharing files not tied to a specific message

### Get All Files in Chat
```
GET /files/{chatId}
```
**Returns:** List<ChatAttachmentDto> (all files: message attachments + shared files)
**Authorization:** Must be participant

### Get Shared Files in Chat
```
GET /files/{chatId}/shared
```
**Returns:** List<ChatAttachmentDto> (only shared files, excludes message attachments)
**Authorization:** Must be participant

### Get Teacher's Shared Files
```
GET /files/teacher/{teacherId}
```
**Returns:** List<ChatAttachmentDto> (all files teacher has shared)
**Authorization:** 
- Teachers: Can view own files
- Others: Can view if they have chat with teacher

### Delete File
```
DELETE /files/{attachmentId}
```
**Returns:** 204 No Content
**Authorization:** Only the uploader can delete
**Behavior:** Removes file from storage and database

---

## Status Codes

### Success
- `200 OK` - Request successful, returns data
- `201 Created` - Resource created, returns data
- `204 No Content` - Request successful, no data returned

### Client Errors
- `400 Bad Request` - Invalid input or validation failed
- `401 Unauthorized` - Missing or invalid JWT token
- `403 Forbidden` - User doesn't have permission
- `404 Not Found` - Resource doesn't exist

### Server Errors
- `500 Internal Server Error` - Server error

---

## Example Workflows

### Teacher Creates Chat with Parent
```
1. POST /chats
   {
     "chatType": "ParentChat",
     "parentId": "parent-guid"
   }
   → Returns ChatDto with chatId

2. POST /messages/{chatId}
   Content: "Hello, I'd like to discuss your child's progress"
   → Returns MessageDto

3. GET /chats
   → Shows chat in list with last message preview
```

### Teacher and Parent Exchange Files
```
1. POST /files/{chatId}
   → Teacher uploads curriculum PDF
   → Returns ChatAttachmentDto

2. POST /messages/{chatId}
   content: "Here's the curriculum"
   attachments: [progress_report.pdf]
   → Returns MessageDto with attachments

3. GET /files/{chatId}/shared
   → Both see shared PDFs
   
4. GET /files/{chatId}
   → Both see all files (shared + message attachments)
```

### Get Messages with Pagination
```
1. GET /messages/{chatId}?pageNumber=1&pageSize=20
   → Returns first 20 messages

2. GET /messages/{chatId}?pageNumber=2&pageSize=20
   → Returns next 20 messages

3. GET /messages/{chatId}/recent?limit=10
   → Returns last 10 messages (for sync)
```

### Search in Chat
```
GET /messages/{chatId}/search?searchTerm=homework
→ Returns all messages containing "homework"
```

---

## Common Errors

### 401 Unauthorized
```json
{
  "error": "Missing or invalid token"
}
```
**Fix:** Ensure Bearer token is provided in Authorization header

### 403 Forbidden
```json
{
  "error": "User doesn't have access to this chat"
}
```
**Fix:** User must be a participant in the chat

### 400 Bad Request - File Upload
```json
{
  "error": "File exceeds maximum size of 50MB"
}
```
**Fixes:**
- Check file size
- Update FILE_STORAGE_MAX_SIZE_MB in .env if needed

```json
{
  "error": "File type .exe is not allowed"
}
```
**Fix:** Use allowed extensions: .pdf, .doc, .docx, .jpg, .png, .xlsx, .txt, .zip

### 404 Not Found
```json
{
  "error": "Chat not found"
}
```
**Fix:** Verify chatId is correct and you have access

---

## Response Models

### ChatDto
```json
{
  "chatId": "guid",
  "chatType": "ParentChat|StudentChat",
  "teacherId": "guid",
  "parentId": "guid|null",
  "studentId": "guid|null",
  "participantName": "Jane Smith",
  "participantProfilePicture": "url|null",
  "lastMessage": "Thanks for your help!",
  "lastMessageTime": "2026-01-27T10:30:00Z",
  "unreadCount": 0,
  "createdAt": "2026-01-20T08:00:00Z",
  "updatedAt": "2026-01-27T10:30:00Z",
  "isActive": true
}
```

### MessageDto
```json
{
  "messageId": "guid",
  "chatId": "guid",
  "senderId": "guid",
  "senderName": "John Doe",
  "senderProfilePicture": "url|null",
  "content": "Message text",
  "createdAt": "2026-01-27T10:30:00Z",
  "attachments": [
    {
      "attachmentId": "guid",
      "fileName": "document.pdf",
      "fileUrl": "/uploads/chats/chat-id/document_guid.pdf",
      "fileSize": 2048,
      "fileType": "application/pdf",
      "uploadedBy": "guid",
      "uploadedByName": "Jane Smith",
      "createdAt": "2026-01-27T10:25:00Z"
    }
  ]
}
```

### ChatAttachmentDto
```json
{
  "attachmentId": "guid",
  "fileName": "homework.pdf",
  "fileUrl": "/uploads/chats/chat-id/homework_guid.pdf",
  "fileSize": 1024,
  "fileType": "application/pdf",
  "uploadedBy": "guid",
  "uploadedByName": "John Doe",
  "createdAt": "2026-01-27T10:30:00Z"
}
```

### PaginatedMessagesDto
```json
{
  "messages": [...],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 50,
  "totalPages": 3
}
```

---

## Testing with Swagger

1. Navigate to `http://localhost:5000/swagger/index.html`
2. Click on endpoint to expand
3. Click "Try it out"
4. For endpoints requiring token:
   - Click lock icon on endpoint
   - Paste JWT token in format: `Bearer <token>`
5. Enter parameters and execute

---

## Frontend Integration Notes

### Chat List View
- Use: `GET /chats`
- Display participantName, lastMessage, lastMessageTime
- Click to open chat detail

### Chat Detail/Messages View
- Use: `GET /chats/{chatId}` (with messages)
- Or: `GET /messages/{chatId}?pageNumber=X&pageSize=20` (for pagination)
- Use: `POST /messages/{chatId}` to send messages
- Handle file uploads in message form

### Send Message with Files
- Use multipart/form-data
- Include content and attachments array
- Show progress for file uploads

### Shared Files Section
- Use: `GET /files/{chatId}/shared` for dedicated shared files view
- Use: `GET /files/{chatId}` for all files view
- Allow upload via: `POST /files/{chatId}`

### Real-Time Sync
- Periodically call: `GET /messages/{chatId}/recent?limit=10`
- Or implement SignalR for true real-time (future enhancement)

---

## Configuration

### Environment Variables (Optional)
```
FILE_STORAGE_LOCAL_PATH=uploads/chats
FILE_STORAGE_MAX_SIZE_MB=50
FILE_STORAGE_ALLOWED_EXTENSIONS=.pdf,.doc,.docx,.jpg,.png,.xlsx,.txt,.zip
```

### Default Pagination
- Page size: 20 messages
- Maximum page size: 100 messages
- First page: pageNumber=1

---

## Security Considerations

✅ **JWT Required**: All endpoints require valid Bearer token
✅ **Role-Based Access**: Different permissions for Teacher/Parent/Student
✅ **Participant Verification**: User must be participant in chat
✅ **Uploader Verification**: Only uploaders can delete files
✅ **File Validation**: Type, size, name sanitization
✅ **Soft Delete**: Archived chats not deleted, just marked inactive
✅ **Read-Only**: No message editing/deletion (immutable messages)
