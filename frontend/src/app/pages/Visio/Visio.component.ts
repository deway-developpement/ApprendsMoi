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

  private readonly signatureEndpoint = 'http://localhost:5254/api/zoom/signature';
  
  zoomMeetingUrl = 'https://us05web.zoom.us/j/86098037935?pwd=P8vUnsgeTHza0RDKBUwDsOwobRxTR1.1';
  isInitializingSdk = false;
  sdkReady = false;
  sdkError = '';
  isFetchingSignature = false;

  zoomSdkConfig = {
    sdkKey: '',
    signature: '',
    meetingNumber: '86098037935',
    userName: 'Invite ApprendsMoi',
    userEmail: '',
    passWord: 'P8vUnsgeTHza0RDKBUwDsOwobRxTR1.1',
    role: 0
  };

  ngAfterViewInit(): void {
    // SDK initialized on button click
  }

  openZoomInNewTab() {
    if (typeof window !== 'undefined') {
      window.open(this.zoomMeetingUrl, '_blank', 'noopener,noreferrer');
    }
  }

  async initZoomSdk(): Promise<void> {
    try {
      await this.ensureSignature();
      this.tryInitZoomSdk();
    } catch (err) {
      this.sdkError = 'Impossible de recuperer la signature du serveur.';
      console.error('Signature fetch error:', err);
    }
  }

  private async ensureSignature(): Promise<void> {
    if (this.zoomSdkConfig.signature) return;
    this.isFetchingSignature = true;
    this.sdkError = '';

    const body = {
      meetingNumber: this.zoomSdkConfig.meetingNumber,
      role: this.zoomSdkConfig.role
    };

    const res = await fetch(this.signatureEndpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    this.isFetchingSignature = false;

    if (!res.ok) {
      const msg = await res.text();
      throw new Error(msg || 'Erreur serveur');
    }

    const data = await res.json();
    this.zoomSdkConfig.signature = data.signature;
    if (data.sdkKey) {
      this.zoomSdkConfig.sdkKey = data.sdkKey;
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
      });
  }

  private formatZoomError(err: any): string {
    let msg = 'Defaut de se joindre a la reunion.';
    if (err?.reason) msg += ` ${err.reason}`;
    if (err?.errorCode) msg += ` (Code: ${err.errorCode})`;
    return msg;
  }
}
