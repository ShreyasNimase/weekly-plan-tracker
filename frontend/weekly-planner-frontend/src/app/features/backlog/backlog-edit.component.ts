import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BacklogService } from '../../core/services/backlog.service';
import { BacklogCategory, BacklogPriority } from '../../shared/enums/status.enum';

@Component({
  selector: 'app-backlog-edit',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './backlog-edit.component.html',
  styleUrl: './backlog-edit.component.scss',
})
export class BacklogEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly backlogService = inject(BacklogService);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly editId = signal<string | null>(null);
  readonly itemStatus = signal<string>('');

  readonly categories: { value: BacklogCategory; label: string }[] = [
    { value: BacklogCategory.Feature, label: 'Feature' },
    { value: BacklogCategory.Bug, label: 'Bug' },
    { value: BacklogCategory.TechDebt, label: 'Tech Debt' },
    { value: BacklogCategory.Learning, label: 'Learning' },
    { value: BacklogCategory.Other, label: 'Other' },
  ];

  readonly priorities: { value: BacklogPriority; label: string }[] = [
    { value: BacklogPriority.High, label: '🔴 High' },
    { value: BacklogPriority.Medium, label: '🟡 Medium' },
    { value: BacklogPriority.Low, label: '🟢 Low' },
  ];

  readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    description: [''],
    category: ['' as BacklogCategory, Validators.required],
    priority: [BacklogPriority.Medium as BacklogPriority, Validators.required],
    estimatedHours: [null as number | null, [Validators.min(0.5), Validators.max(200)]],
  });

  get isEditMode(): boolean { return !!this.editId(); }
  get pageTitle(): string { return this.isEditMode ? 'Edit Backlog Item' : 'Add a New Backlog Item'; }
  get saveLabel(): string { return this.isEditMode ? 'Save Changes' : 'Save This Item'; }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.editId.set(id);
      this.loadItem(id);
    }
  }

  private loadItem(id: string): void {
    this.isLoading.set(true);
    this.backlogService.getById(id).subscribe({
      next: (item) => {
        this.itemStatus.set(item.status);
        this.form.patchValue({
          title: item.title,
          description: item.description ?? '',
          category: item.category as BacklogCategory,
          priority: item.priority as BacklogPriority,
          estimatedHours: item.estimatedHours ?? null,
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load item.', 'Close', { duration: 4000 });
        this.router.navigate(['/backlog']);
      },
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving.set(true);

    const v = this.form.value;
    const payload = {
      title: v.title!,
      description: v.description || undefined,
      category: v.category!,
      priority: v.priority!,
      estimatedHours: v.estimatedHours ?? undefined,
    };

    const call$ = this.isEditMode
      ? this.backlogService.update(this.editId()!, payload)
      : this.backlogService.create(payload);

    call$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.isEditMode ? 'Changes saved!' : 'Backlog item saved!', 'Close', { duration: 3000 });
        this.router.navigate(['/backlog']);
      },
      error: (err) => {
        this.isSaving.set(false);
        this.snackBar.open(err?.error?.message ?? 'Save failed.', 'Close', { duration: 5000 });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/backlog']);
  }
}
