import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { BacklogItem } from '../../shared/models/backlog-item.model';
import { BacklogService } from '../../core/services/backlog.service';
import { BacklogCategory } from '../../shared/enums/status.enum';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';

export const CATEGORY_META: Record<string, { label: string; cls: string }> = {
  CLIENT_FOCUSED: { label: 'Client Focused', cls: 'cat-client' },
  TECH_DEBT: { label: 'Tech Debt', cls: 'cat-techdebt' },
  R_AND_D: { label: 'R\u0026D', cls: 'cat-rnd' },
  // legacy keys kept for any old data
  Feature: { label: 'Client Focused', cls: 'cat-client' },
  TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
  Learning: { label: 'R\u0026D', cls: 'cat-rnd' },
  Bug: { label: 'Bug', cls: 'cat-techdebt' },
  Other: { label: 'Other', cls: 'cat-rnd' },
};

const STATUS_OPTIONS = [
  { value: '', label: 'Show All' },
  { value: 'AVAILABLE', label: 'Available Only' },
  { value: 'IN_PLAN', label: 'In Current Plan' },
  { value: 'COMPLETED', label: 'Completed' },
];

/** Only the 3 primary filter categories shown in UI */
const DISPLAY_CATEGORIES = ['CLIENT_FOCUSED', 'TECH_DEBT', 'R_AND_D'];

@Component({
  selector: 'app-backlog',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDividerModule,
    MatDialogModule,
    NgClass,
  ],
  templateUrl: './backlog.component.html',
  styleUrl: './backlog.component.scss',
})
export class BacklogComponent implements OnInit {
  private readonly backlogService = inject(BacklogService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly allItems = signal<BacklogItem[]>([]);
  readonly isLoading = signal(true);

  // ── Filter state ──────────────────────────────────────────────
  readonly activeCategories = signal<Set<string>>(new Set());
  selectedStatus = 'AVAILABLE';   // default: show Available items only
  searchTerm = '';

  readonly statusOptions = STATUS_OPTIONS;
  readonly categories = DISPLAY_CATEGORIES;   // Only 3 shown in filter bar
  readonly categoryMeta = CATEGORY_META;

  // ── Computed filtered list ────────────────────────────────────
  readonly filteredItems = computed(() => {
    const cats = this.activeCategories();
    const status = this.selectedStatus.toLowerCase();
    const search = this.searchTerm.trim().toLowerCase();
    return this.allItems().filter((item) => {
      if (cats.size > 0 && !cats.has(item.category)) return false;
      if (status && item.status.toLowerCase() !== status) return false;
      if (search && !item.title.toLowerCase().includes(search)) return false;
      return true;
    });
  });

  readonly archivedCount = computed(
    () => this.allItems().filter((i) => i.status?.toUpperCase() === 'ARCHIVED').length
  );

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.isLoading.set(true);
    this.backlogService.getAll().subscribe({
      next: (items) => { this.allItems.set(items); this.isLoading.set(false); },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load backlog', 'Close', { duration: 4000 });
      },
    });
  }

  // ── Category toggle (multi-select) ────────────────────────────
  toggleCategory(cat: string): void {
    this.activeCategories.update((s) => {
      const next = new Set(s);
      next.has(cat) ? next.delete(cat) : next.add(cat);
      return next;
    });
  }

  isCatActive(cat: string): boolean {
    return this.activeCategories().has(cat);
  }

  showArchived(): void {
    this.selectedStatus = 'ARCHIVED';
  }

  clearFilters(): void {
    this.activeCategories.set(new Set());
    this.selectedStatus = 'AVAILABLE';
    this.searchTerm = '';
  }

  // ── Navigation ────────────────────────────────────────────────
  addNew(): void {
    this.router.navigate(['/backlog', 'edit']);
  }

  editItem(item: BacklogItem): void {
    this.router.navigate(['/backlog', 'edit', item.id]);
  }

  // ── Archive with confirm dialog ───────────────────────────────
  archiveItem(item: BacklogItem): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: `Archive "${item.title}"?`,
        message: 'It will be moved to the archived list.',
        confirmText: 'Yes, Archive It',
        confirmColor: 'primary',
        icon: 'archive',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.backlogService.archive(item.id).subscribe({
        next: () => {
          this.snackBar.open(`"${item.title}" archived`, 'Close', { duration: 3000 });
          this.loadAll();
        },
        error: (err) => this.snackBar.open(err?.error?.message ?? 'Archive failed', 'Close', { duration: 4000 }),
      });
    });
  }

  // ── Display helpers ───────────────────────────────────────────
  getCatMeta(category: string) {
    return CATEGORY_META[category] ?? { label: category, cls: 'cat-other' };
  }

  get hasActiveFilters(): boolean {
    return this.activeCategories().size > 0
      || this.selectedStatus !== 'Available'
      || this.searchTerm.trim() !== '';
  }
}
