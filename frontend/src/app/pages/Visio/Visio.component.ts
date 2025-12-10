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
  @ViewChild('zoomContainer', { static: true }) zoomContainer?: ElementRef<HTMLDivElement>;

  zoomMeetingUrl =
    'https://us05web.zoom.us/j/83674076204?pwd=BsoqNVqAwVoADMWsUA0ZX3Jxns2iw8.0';
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
    meetingNumber: '83674076204',
    userName: 'Invite ApprendsMoi',
    userEmail: '',
    passWord: 'BsoqNVqAwVoADMWsUA0ZX3Jxns2iw8.0',
    role: 0 // 0 = participant, 1 = host
  };

  constructor(private sanitizer: DomSanitizer) {
    this.safeZoomUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.zoomMeetingUrl);
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
      await this.ensureSignature();
      this.tryInitZoomSdk(true);
    } catch (err) {
      this.sdkError = 'Impossible de recuperer la signature du serveur.';
      console.error(err);
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
    if (!this.zoomContainer) return;
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

      client
        .init({
          debug: true,
          zoomAppRoot: rootElement,
          language: 'fr-FR'
        })
        .then(() =>
          client.join({
            sdkKey: this.zoomSdkConfig.sdkKey,
            signature: this.zoomSdkConfig.signature,
            meetingNumber: this.zoomSdkConfig.meetingNumber,
            password: this.zoomSdkConfig.passWord,
            userName: this.zoomSdkConfig.userName,
            userEmail: this.zoomSdkConfig.userEmail
          })
        )
        .then(() => {
          this.sdkReady = true;
          this.isInitializingSdk = false;
        })
        .catch((err: unknown) => {
          this.isInitializingSdk = false;
          this.sdkError =
            'Impossible de lancer le SDK Zoom. Verifiez la signature, le SDK Key et que le SDK est charge.';
          console.error('Zoom SDK error', err);
        });
    } catch (error) {
      this.isInitializingSdk = false;
      this.sdkError =
        'Erreur lors de l initialisation du SDK Zoom. Verifiez que le script CDN est present.';
      console.error(error);
    }
  }
}
