import { Component, inject, signal } from '@angular/core';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DataService } from '../../core/services/data.service';

@Component({
    selector: 'app-import-dialog',
    standalone: true,
    imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
    template: `
    <div class="import-dialog">
      <div class="import-header">
        <mat-icon class="import-icon">upload_file</mat-icon>
        <h2 mat-dialog-title>Load Data from File</h2>
      </div>

      <mat-dialog-content>
        <p class="import-desc">
          Select a <strong>.json</strong> backup file previously downloaded from this app.
          All current data will be replaced with the backup contents.
        </p>

        <!-- File picker area -->
        <div class="drop-zone" [class.has-file]="selectedFile()" (click)="filePicker.click()">
          <mat-icon class="drop-icon">{{ selectedFile() ? 'description' : 'folder_open' }}</mat-icon>
          <span class="drop-text">
            {{ selectedFile() ? selectedFile()!.name : 'Click to choose a JSON file' }}
          </span>
          <input #filePicker type="file" accept=".json" hidden (change)="onFileSelected($event)" />
        </div>

        @if (error()) {
          <div class="import-error">
            <mat-icon>error_outline</mat-icon>
            {{ error() }}
          </div>
        }

        <div class="import-warning">
          <mat-icon>warning</mat-icon>
          <span>This will replace all existing data. This action cannot be undone.</span>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-stroked-button [mat-dialog-close]="false" [disabled]="isImporting()">
          Cancel
        </button>
        <button mat-flat-button color="primary"
                [disabled]="!selectedFile() || isImporting()"
                (click)="confirmImport()"
                class="import-btn">
          @if (isImporting()) {
            <mat-spinner diameter="16"></mat-spinner>
          } @else {
            <mat-icon>upload</mat-icon>
          }
          Confirm Replace Data
        </button>
      </mat-dialog-actions>
    </div>
  `,
    styles: [`
    .import-dialog { padding: 8px; min-width: 380px; }
    .import-header { display: flex; align-items: center; gap: 12px; padding: 8px 0 0; }
    h2 { margin: 0; font-size: 1.05rem; font-weight: 700; }
    .import-icon { font-size: 28px; width: 28px; height: 28px; color: #2e7d32; }
    .import-desc { font-size: 0.9rem; line-height: 1.6; margin: 0 0 16px; color: #555; }

    .drop-zone {
      border: 2px dashed #ccc;
      border-radius: 12px;
      padding: 24px 16px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
      margin-bottom: 16px;
      &:hover { border-color: #1565c0; background: rgba(21,101,192,0.04); }
      &.has-file { border-color: #2e7d32; background: rgba(46,125,50,0.04); }
    }
    .drop-icon { display: block; font-size: 36px; width: 36px; height: 36px; margin: 0 auto 8px; color: #888; }
    .drop-text { font-size: 0.88rem; color: #555; word-break: break-all; }

    .import-error {
      display: flex; align-items: center; gap: 6px;
      background: #ffebee; color: #b71c1c;
      border-radius: 8px; padding: 10px 14px; margin-bottom: 12px;
      font-size: 0.85rem;
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }
    .import-warning {
      display: flex; align-items: flex-start; gap: 6px;
      color: #e65100; font-size: 0.82rem; line-height: 1.4;
      mat-icon { font-size: 16px; width: 16px; height: 16px; flex-shrink: 0; margin-top: 1px; }
    }

    mat-dialog-actions { padding-top: 16px; gap: 8px; }
    .import-btn {
      border-radius: 8px !important;
      display: flex !important; align-items: center !important; gap: 6px;
      mat-spinner { display: inline-block; }
    }
  `],
})
export class ImportDialogComponent {
    private readonly dataService = inject(DataService);
    private readonly dialogRef = inject(MatDialogRef<ImportDialogComponent>);

    readonly selectedFile = signal<File | null>(null);
    readonly isImporting = signal(false);
    readonly error = signal<string | null>(null);

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0] ?? null;
        this.selectedFile.set(file);
        this.error.set(null);
    }

    confirmImport(): void {
        const file = this.selectedFile();
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (e) => {
            let json: unknown;
            try {
                json = JSON.parse(e.target?.result as string);
            } catch {
                this.error.set('File is not valid JSON. Please choose a correct backup file.');
                return;
            }

            // Client-side validation
            const obj = json as Record<string, unknown>;
            if (obj['appName'] !== 'WeeklyPlanTracker') {
                this.error.set('Invalid backup file: not a WeeklyPlanTracker export.');
                return;
            }

            this.isImporting.set(true);
            this.dataService.uploadBackup(json).subscribe({
                next: () => {
                    this.isImporting.set(false);
                    this.dialogRef.close(true); // signal success to parent
                },
                error: (err) => {
                    this.isImporting.set(false);
                    this.error.set(err?.error?.message ?? 'Import failed. Please check the file and try again.');
                },
            });
        };
        reader.readAsText(file);
    }
}
