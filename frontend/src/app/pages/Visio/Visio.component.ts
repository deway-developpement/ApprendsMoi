import { AfterViewInit, Component, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { HeaderComponent } from '../../components/Header/header.component';
import { ButtonComponent } from '../../components/shared/Button/button.component';

// Zoom Web SDK (Component view) expected as a global when loaded from CDN.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
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

  // IMPORTANT: Update this with YOUR actual Zoom meeting details
  // To get the meeting details:
  // 1. Schedule a meeting in Zoom
  // 2. Get the meeting ID (numeric only, no spaces or dashes)
  // 3. If password protected, get the NUMERIC passcode (not the URL pwd= parameter)
  // 4. Make sure the meeting is either started or has "join before host" enabled
  zoomMeetingUrl =
    'https://us05web.zoom.us/j/86098037935?pwd=P8vUnsgeTHza0RDKBUwDsOwobRxTR1.1';
  safeZoomUrl: SafeResourceUrl;
  iframeLoaded = false;
  isInitializingSdk = false;
  sdkReady = false;
  sdkError = '';
  isFetchingSignature = false;
  // Point this to your backend (adjust port if your API runs elsewhere).
  signatureEndpoint = 'http://localhost:5254/api/zoom/signature';

  zoomSdkConfig = {
    sdkKey: '',
    signature: '',
    meetingNumber: '86098037935',  // Update this with your meeting ID
    userName: 'Invite ApprendsMoi',
    userEmail: '',
    // For the Web SDK, password is often optional if the signature is valid
    // If needed, use the NUMERIC passcode from Zoom (not the URL pwd parameter)
    // Example: if your meeting passcode is 123456, use '123456'
    // Leave empty ('') if no password is required
    passWord: 'P8vUnsgeTHza0RDKBUwDsOwobRxTR1.1',  
    role: 0 // 0 = participant, 1 = host
  };

  constructor(private sanitizer: DomSanitizer) {
    this.safeZoomUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.zoomMeetingUrl);
  }

  private decodePassword(urlEncodedPwd: string): string {
    try {
      return decodeURIComponent(urlEncodedPwd);
    } catch {
      return urlEncodedPwd; // Return as-is if decode fails
    }
  }

  ngAfterViewInit(): void {
    // Avoid auto-init to prevent change detection errors before scripts are ready; use the button instead.
  }

  onZoomLoad() {
    this.iframeLoaded = true;
  }

  openZoomInNewTab() {
    if (typeof window !== 'undefined') {
      window.open(this.zoomMeetingUrl, '_blank', 'noopener,noreferrer');
    }
  }

  async initZoomSdk(): Promise<void> {
    try {
      console.log('Fetching signature for meeting:', this.zoomSdkConfig.meetingNumber);
      await this.ensureSignature();
      console.log('Signature received, initializing SDK...');
      console.log('Config:', {
        meetingNumber: this.zoomSdkConfig.meetingNumber,
        userName: this.zoomSdkConfig.userName,
        role: this.zoomSdkConfig.role,
        hasPassword: !!this.zoomSdkConfig.passWord,
        hasSdkKey: !!this.zoomSdkConfig.sdkKey,
        hasSignature: !!this.zoomSdkConfig.signature
      });
      this.tryInitZoomSdk(true);
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

  private tryInitZoomSdk(force = false): void {
    if (!this.zoomContainer) {
      console.error('Zoom container not found');
      this.sdkError = 'Container Zoom non trouve';
      return;
    }
    if (!force && this.sdkReady) return;

    if (typeof ZoomMtgEmbedded === 'undefined') {
      this.sdkError =
        'SDK Zoom non charge. Ajoutez le script Web Meeting SDK (Component view) via CDN avant d initialiser.';
      return;
    }

    this.isInitializingSdk = true;
    this.sdkError = '';

    try {
      const client = ZoomMtgEmbedded.createClient();
      const rootElement = this.zoomContainer.nativeElement;

      // Ensure the container has proper dimensions
      rootElement.style.width = '100%';
      rootElement.style.height = '600px';

      client
        .init({
          debug: true,
          zoomAppRoot: rootElement,
          language: 'fr-FR',
          leaveUrl: window.location.origin,
          customize: {
            video: {
              isResizable: true,
              viewSizes: {
                default: {
                  width: rootElement.offsetWidth,
                  height: rootElement.offsetHeight
                }
              }
            }
          }
        })
        .then(() => {
          console.log('Zoom SDK initialized, joining meeting...');
          
          const joinConfig: any = {
            sdkKey: this.zoomSdkConfig.sdkKey,
            signature: this.zoomSdkConfig.signature,
            meetingNumber: this.zoomSdkConfig.meetingNumber,
            userName: this.zoomSdkConfig.userName
          };
          
          // Only add password if it's not empty
          if (this.zoomSdkConfig.passWord) {
            joinConfig.password = this.zoomSdkConfig.passWord;
          }
          
          // Add userEmail if provided
          if (this.zoomSdkConfig.userEmail) {
            joinConfig.userEmail = this.zoomSdkConfig.userEmail;
          }
          
          console.log('Join config:', { ...joinConfig, signature: '***' });
          return client.join(joinConfig);
        })
        .then(() => {
          this.sdkReady = true;
          this.isInitializingSdk = false;
          console.log('Successfully joined Zoom meeting');
        })
        .catch((err: unknown) => {
          this.isInitializingSdk = false;
          console.error('Zoom SDK error details:', err);
          
          // Try to extract more specific error message
          let errorMsg = 'Defaut de se joindre a la reunion.';
          if (err && typeof err === 'object') {
            const error = err as any;
            if (error.reason) errorMsg += ` Raison: ${error.reason}`;
            if (error.errorMessage) errorMsg += ` Message: ${error.errorMessage}`;
            if (error.errorCode) errorMsg += ` Code: ${error.errorCode}`;
          }
          
          this.sdkError = errorMsg;
        });
    } catch (error) {
      this.isInitializingSdk = false;
      this.sdkError =
        'Erreur lors de l initialisation du SDK Zoom. Verifiez que le script CDN est present.';
      console.error(error);
    }
  }
}
