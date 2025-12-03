import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-text-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './text-input.component.html',
  styleUrls: ['./text-input.component.scss'],
})
export class TextInputComponent {
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() type: 'text' | 'email' | 'password' | 'search' = 'text';
  @Input() value: string = '';
  @Input() error: string | null = null;
  @Input() disabled: boolean = false;
  @Input() fullWidth: boolean = true;
  @Input() iconLeft?: string;   // ex: 'search', 'mail', etc. (optionnel)

  @Output() valueChange = new EventEmitter<string>();
  @Output() enter = new EventEmitter<void>();

  onInputChange(newValue: string) {
    this.value = newValue;
    this.valueChange.emit(newValue);
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.enter.emit();
    }
  }
}
