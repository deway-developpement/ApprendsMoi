import { Component, ElementRef, ViewChild, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';

declare const ZoomMtgEmbedded: any;

@Component({
  standalone: true,
  selector: 'app-visio',
  templateUrl: './Visio.component.html',
  styleUrls: ['./Visio.component.scss'],
  imports: [CommonModule, HeaderComponent, ButtonComponent]
})
export class Visio implements OnInit, OnDestroy {
  @ViewChild('zoomContainer', { static: false }) zoomContainer?: ElementRef<HTMLDivElement>;

  private readonly apiBaseUrl = 'http://localhost:5254/api/zoom';
  
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

    try {
      this.isLoadingMeeting = true;
      this.sdkError = '';

      const response = await fetch(`${this.apiBaseUrl}/meetings/${this.meetingId}`);

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Réunion non trouvée');
      }

      const data = await response.json();
      
      // Update config with meeting data
      this.zoomSdkConfig.meetingNumber = data.meetingNumber;
      this.zoomSdkConfig.passWord = data.password || '';
      this.participantSignature = data.participantSignature || '';
      this.zoomSdkConfig.signature = this.participantSignature;
      this.zoomSdkConfig.sdkKey = data.sdkKey;
      this.zoomMeetingUrl = data.joinUrl;
      this.hasRetriedAsParticipant = false;

      this.isLoadingMeeting = false;
      this.cdr.detectChanges();

      // Auto-initialize SDK
      this.tryInitZoomSdk();
    } catch (err) {
      this.isLoadingMeeting = false;
      this.sdkError = err instanceof Error ? err.message : 'Erreur lors du chargement de la réunion';
      console.error('Meeting loading error:', err);
    }
  }

  async createAndInitMeeting(): Promise<void> {
    try {
      this.isLoadingMeeting = true;
      this.sdkError = '';

      const response = await fetch(`${this.apiBaseUrl}/meeting`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ topic: 'ApprendsMoi - Session de classe' })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Erreur lors de la création de la réunion');
      }

      const data = await response.json();
      
      // Update config with meeting data
      this.zoomSdkConfig.meetingNumber = data.meetingNumber;
      this.zoomSdkConfig.passWord = data.password || '';
      this.participantSignature = data.participantSignature || '';
      this.zoomSdkConfig.signature = this.participantSignature;
      this.zoomSdkConfig.sdkKey = data.sdkKey;
      this.zoomMeetingUrl = data.joinUrl;
      this.hasRetriedAsParticipant = false;

      this.isLoadingMeeting = false;
      this.cdr.detectChanges();

      // Auto-initialize SDK
      this.tryInitZoomSdk();
    } catch (err) {
      this.isLoadingMeeting = false;
      this.sdkError = err instanceof Error ? err.message : 'Erreur lors de la création de la réunion';
      console.error('Meeting creation error:', err);
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
      this.sdkError = 'Container Zoom non trouvé';
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

  private formatZoomError(err: any): string {
    let msg = 'Défaut de se joindre a la reunion.';
    if (err?.reason) msg += ` ${err.reason}`;
    if (err?.errorCode) msg += ` (Code: ${err.errorCode})`;
    return msg;
  }
}
