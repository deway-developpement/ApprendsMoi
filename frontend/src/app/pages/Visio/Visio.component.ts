import { Component, ElementRef, ViewChild, OnInit, OnDestroy, AfterViewInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { environment } from '../../environments/environment';

declare const ZoomMtgEmbedded: any;

interface MeetingDetailsResponse {
  meetingNumber: string;
  password: string;
  participantSignature: string;
  sdkKey: string;
  joinUrl: string;
}

interface CreateMeetingResponse extends MeetingDetailsResponse {}

@Component({
  standalone: true,
  selector: 'app-visio',
  templateUrl: './Visio.component.html',
  styleUrls: ['./Visio.component.scss'],
  imports: [CommonModule, HeaderComponent, ButtonComponent]
})
export class Visio implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('zoomContainer', { static: false }) zoomContainer?: ElementRef<HTMLDivElement>;

  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiUrl}/api/zoom`;
  private viewReady = false;
  
  meetingId: number | null = null;
  zoomMeetingUrl = '';
  isInitializingSdk = false;
  sdkReady = false;
  sdkError = '';
  isLoadingMeeting = false;
  participantSignature = '';
  private hasRetriedAsParticipant = false;
  private zoomClient: any = null;

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
    private route: ActivatedRoute
  ) {}

  private generateUniqueUsername(): string {
    const randomId = Math.floor(Math.random() * 100000);
    const timestamp = Date.now().toString().slice(-6);
    return `Participant-${randomId}-${timestamp}`;
  }

  async ngOnInit(): Promise<void> {
    // Get meeting ID from route parameter
    this.route.params.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.meetingId = parseInt(id, 10);
        // Defer to next tick to avoid ExpressionChangedAfterItHasBeenCheckedError
        setTimeout(() => this.loadMeetingAndInit(), 0);
      } else {
        this.sdkError = 'ID de réunion manquant';
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    if (this.zoomSdkConfig.meetingNumber && !this.sdkReady) {
      this.tryInitZoomSdk();
    }
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
  }

  openZoomInNewTab() {
    if (this.zoomMeetingUrl && typeof window !== 'undefined') {
      window.open(this.zoomMeetingUrl, '_blank', 'noopener,noreferrer');
    }
  }

  async loadMeetingAndInit(): Promise<void> {
    if (!this.meetingId) {
      this.sdkError = 'ID de réunion invalide';
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

      this.cdr.detectChanges();

      // Auto-initialize SDK
      if (this.viewReady) {
        this.tryInitZoomSdk();
      }
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

      this.cdr.detectChanges();

      // Auto-initialize SDK
      if (this.viewReady) {
        this.tryInitZoomSdk();
      }
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
        this.sdkError = 'Container Zoom non trouvé';
      }
      return;
    }

    if (this.sdkReady) return;

    if (typeof ZoomMtgEmbedded === 'undefined') {
      this.sdkError = 'SDK Zoom non chargé.';
      return;
    }

    this.isInitializingSdk = true;
    this.sdkError = '';

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
            isResizable: true,
            viewSizes: {
              default: {
                width: 1000,
                height: 600
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
