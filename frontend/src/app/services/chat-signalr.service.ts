import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { MessageDto } from './chat.service';

@Injectable({
  providedIn: 'root'
})
export class ChatSignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  
  // Observable subjects for chat events
  private messageReceivedSubject = new BehaviorSubject<MessageDto | null>(null);
  public messageReceived$ = this.messageReceivedSubject.asObservable();

  private userTypingSubject = new BehaviorSubject<string | null>(null);
  public userTyping$ = this.userTypingSubject.asObservable();

  private userStoppedTypingSubject = new BehaviorSubject<void>(undefined);
  public userStoppedTyping$ = this.userStoppedTypingSubject.asObservable();

  private connectionStatusSubject = new BehaviorSubject<boolean>(false);
  public connectionStatus$ = this.connectionStatusSubject.asObservable();

  constructor() {
    this.initConnection();
  }

  private initConnection(): void {
    // Extract base URL without /api
    const apiUrl = environment.apiUrl;
    const baseUrl = apiUrl.replace('/api', '');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/chat`, {
        accessTokenFactory: () => {
          const token = localStorage.getItem('token');
          return token || '';
        },
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.previousRetryCount === 0) {
            return 0; // Retry immediately on first attempt
          } else if (retryContext.previousRetryCount < 5) {
            return 1000; // Retry every 1 second for next 5 attempts
          } else {
            return 5000; // After that, retry every 5 seconds
          }
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Connection event handlers
    this.hubConnection.on('ReceiveMessage', (message: MessageDto) => {
      console.log('Message received via SignalR:', message);
      this.messageReceivedSubject.next(message);
    });

    this.hubConnection.on('UserTyping', (userName: string) => {
      console.log(`${userName} is typing...`);
      this.userTypingSubject.next(userName);
    });

    this.hubConnection.on('UserStoppedTyping', () => {
      console.log('User stopped typing');
      this.userStoppedTypingSubject.next();
    });

    // Connection state change handlers
    this.hubConnection.onreconnecting(() => {
      console.warn('SignalR reconnecting...');
      this.connectionStatusSubject.next(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.connectionStatusSubject.next(true);
    });

    this.hubConnection.onclose(() => {
      console.warn('SignalR disconnected');
      this.connectionStatusSubject.next(false);
    });
  }

  /**
   * Start the SignalR connection
   */
  public connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }

    return this.hubConnection!.start()
      .then(() => {
        console.log('SignalR connected');
        this.connectionStatusSubject.next(true);
      })
      .catch(err => {
        console.error('SignalR connection error:', err);
        return Promise.reject(err);
      });
  }

  /**
   * Stop the SignalR connection
   */
  public disconnect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Disconnected) {
      return Promise.resolve();
    }

    return this.hubConnection!.stop()
      .then(() => {
        console.log('SignalR disconnected');
        this.connectionStatusSubject.next(false);
      })
      .catch(err => {
        console.error('SignalR disconnection error:', err);
        return Promise.reject(err);
      });
  }

  /**
   * Join a chat group
   */
  public joinChat(chatId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Hub connection not initialized');
    }

    return this.hubConnection.invoke('JoinChat', chatId)
      .catch(err => {
        console.error(`Error joining chat ${chatId}:`, err);
        return Promise.reject(err);
      });
  }

  /**
   * Leave a chat group
   */
  public leaveChat(chatId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Hub connection not initialized');
    }

    return this.hubConnection.invoke('LeaveChat', chatId)
      .catch(err => {
        console.error(`Error leaving chat ${chatId}:`, err);
        return Promise.reject(err);
      });
  }

  /**
   * Send message to chat (server will broadcast to group)
   */
  public sendMessageToChat(chatId: string, message: MessageDto): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Hub connection not initialized');
    }

    return this.hubConnection.invoke('SendMessageToChat', chatId, message)
      .catch(err => {
        console.error(`Error sending message to chat ${chatId}:`, err);
        return Promise.reject(err);
      });
  }

  /**
   * Notify others that user is typing
   */
  public notifyTyping(chatId: string, userName: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Hub connection not initialized');
    }

    return this.hubConnection.invoke('UserTyping', chatId, userName)
      .catch(err => {
        console.error(`Error notifying typing in chat ${chatId}:`, err);
        return Promise.reject(err);
      });
  }

  /**
   * Notify others that user stopped typing
   */
  public notifyStoppedTyping(chatId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Hub connection not initialized');
    }

    return this.hubConnection.invoke('UserStoppedTyping', chatId)
      .catch(err => {
        console.error(`Error notifying stopped typing in chat ${chatId}:`, err);
        return Promise.reject(err);
      });
  }

  /**
   * Get connection status
   */
  public isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }
}
