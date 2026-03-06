import {
    Component, Input, Output, EventEmitter
} from '@angular/core';
import { CommonModule, DatePipe, NgFor, NgIf } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Cycle } from '../../models/cycle.model';

@Component({
    selector: 'app-cycle-status-card',
    standalone: true,
    imports: [CommonModule, DatePipe, NgIf, NgFor, MatButtonModule, MatIconModule],
    templateUrl: './cycle-status-card.component.html',
    styleUrl: './cycle-status-card.component.scss',
})
export class CycleStatusCardComponent {
    @Input() cycle!: Cycle;
    @Input() isLead = false;
    @Input() completedHours = 0;

    @Output() setupClick = new EventEmitter<void>();
    @Output() cancelSetupClick = new EventEmitter<void>();
    @Output() freezeReviewClick = new EventEmitter<void>();
    @Output() cancelPlanningClick = new EventEmitter<void>();
    @Output() dashboardClick = new EventEmitter<void>();
    @Output() finishWeekClick = new EventEmitter<void>();

    get stateIcon(): string {
        const icons: Record<string, string> = {
            SETUP: 'pending',
            PLANNING: 'edit_note',
            FROZEN: 'ac_unit',
            COMPLETED: 'task_alt',
        };
        return icons[this.cycle?.state] ?? 'pending';
    }

    get stateLabel(): string {
        const labels: Record<string, string> = {
            SETUP: 'Setting Up',
            PLANNING: 'Planning Open',
            FROZEN: 'In Progress',
            COMPLETED: 'Completed',
        };
        return labels[this.cycle?.state] ?? this.cycle?.state ?? '';
    }

    get progressPct(): number {
        if (!this.cycle?.teamCapacity) return 0;
        return Math.min(100, Math.round((this.completedHours / this.cycle.teamCapacity) * 100));
    }

    /** Returns the date to display in the header (planningDate or weekStartDate) */
    get displayDate(): string | null {
        return this.cycle?.planningDate ?? this.cycle?.weekStartDate ?? null;
    }

    get memberCount(): number {
        return this.cycle?.participatingMemberIds?.length
            ?? this.cycle?.members?.length
            ?? 0;
    }

    catLabel(cat: string): string {
        const labels: Record<string, string> = {
            CLIENT_FOCUSED: 'Client',
            TECH_DEBT: 'Tech Debt',
            R_AND_D: 'R&D',
        };
        return labels[cat] ?? cat;
    }

    onSetupClick(): void { this.setupClick.emit(); }
    onCancelSetupClick(): void { this.cancelSetupClick.emit(); }
    onFreezeReviewClick(): void { this.freezeReviewClick.emit(); }
    onCancelPlanningClick(): void { this.cancelPlanningClick.emit(); }
    onDashboardClick(): void { this.dashboardClick.emit(); }
    onFinishWeekClick(): void { this.finishWeekClick.emit(); }
}
