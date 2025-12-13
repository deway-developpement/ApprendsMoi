import { AfterViewInit, Component, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
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
export class Visio implements AfterViewInit {
  @ViewChild('zoomContainer', { static: false }) zoomContainer?: ElementRef<HTMLDivElement>;

  private readonly apiBaseUrl = 'http://localhost:5254/api/zoom';
  
  zoomMeetingUrl = '';
  isInitializingSdk = false;
  sdkReady = false;
  sdkError = '';
  isCreatingMeeting = false;
  participantSignature = '';
  private hasRetriedAsParticipant = false;

  zoomSdkConfig = {
    sdkKey: '',
    signature: '',
    meetingNumber: '',
    userName: 'Enseignant ApprendsMoi',
    userEmail: '',
    passWord: '',
    role: 1
  };

  async ngAfterViewInit(): Promise<void> {
    // Auto-create meeting when page loads
    await this.createAndInitMeeting();
  }

  openZoomInNewTab() {
    if (this.zoomMeetingUrl && typeof window !== 'undefined') {
      window.open(this.zoomMeetingUrl, '_blank', 'noopener,noreferrer');
    }
  }

  async createAndInitMeeting(): Promise<void> {
    try {
      this.isCreatingMeeting = true;
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
      this.zoomSdkConfig.signature = data.hostSignature || data.signature;
      this.participantSignature = data.participantSignature || '';
      this.zoomSdkConfig.sdkKey = data.sdkKey;
      this.zoomMeetingUrl = data.joinUrl;
      this.hasRetriedAsParticipant = false;

      this.isCreatingMeeting = false;

      // Auto-initialize SDK
      this.tryInitZoomSdk();
    } catch (err) {
      this.isCreatingMeeting = false;
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
      this.sdkError = 'Container Zoom non trouve';
      return;
    }

    if (this.sdkReady) return;

    if (typeof ZoomMtgEmbedded === 'undefined') {
      this.sdkError = 'SDK Zoom non charge.';
      return;
    }

    this.isInitializingSdk = true;
    this.sdkError = '';

    const client = ZoomMtgEmbedded.createClient();
    const container = this.zoomContainer.nativeElement;

    client
      .init({
        zoomAppRoot: container,
        language: 'fr-FR',
        leaveUrl: window.location.origin
      })
      .then(() => client.join({
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
      })
      .catch((err: any) => {
        this.isInitializingSdk = false;
        this.sdkError = this.formatZoomError(err);
        console.error('Zoom SDK error:', err);
        const formattedError = this.formatZoomError(err);

        // Fallback: if host join failed, retry once as participant
        if (!this.hasRetriedAsParticipant && this.participantSignature) {
          this.hasRetriedAsParticipant = true;
          this.zoomSdkConfig.signature = this.participantSignature;
          this.zoomSdkConfig.role = 0;
          this.zoomSdkConfig.userName = 'Participant ApprendsMoi';
          this.tryInitZoomSdk();
          return;
        }

        this.sdkError = formattedError;
      });
  }

  private formatZoomError(err: any): string {
    let msg = 'Defaut de se joindre a la reunion.';
    if (err?.reason) msg += ` ${err.reason}`;
    if (err?.errorCode) msg += ` (Code: ${err.errorCode})`;
    return msg;
  }
}
