import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface SelectOption {
  label: string;
  value: string | number;
}

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './select.component.html',
  styleUrls: ['./select.component.scss'],
})
export class SelectComponent {
  @Input() label: string = '';
  @Input() placeholder: string = 'Sélectionner...';
  @Input() options: SelectOption[] = [];
  @Input() value: string | number | null = null;
  @Input() error: string | null = null;
  @Input() disabled: boolean = false;
  @Input() fullWidth: boolean = true;

  @Output() valueChange = new EventEmitter<string | number | null>();

  onChange(newValue: string | number) {
    this.value = newValue;
    this.valueChange.emit(newValue);
  }
}
