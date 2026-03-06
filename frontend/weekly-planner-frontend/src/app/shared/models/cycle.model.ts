import { BacklogCategory } from '../enums/status.enum';

// Matches ToCycleResponse() shape from CyclesController
export interface CycleMember {
    teamMemberId: string;   // Guid
    name: string;
    allocatedHours: number;
}

export interface CategoryBudget {
    category: string;       // returned as string
    percentage: number;
    hoursBudget: number;
}

export interface Cycle {
    id: string;             // Guid
    weekStartDate: string;  // "yyyy-MM-dd"
    status: string;         // Setup | Planning | Frozen | Completed | Cancelled
    createdAt: string;
    members: CycleMember[];
    categoryBudgets: CategoryBudget[];
}

// POST /api/cycles/start
export interface StartCycleDto {
    weekStartDate: string;  // ISO date string, must be a Tuesday
}

// CategoryBudgetDto used inside SetupCycleDto
export interface CategoryBudgetDto {
    category: BacklogCategory;
    percentage: number;
}

// PUT /api/cycles/{id}/setup
export interface SetupCycleDto {
    memberIds: string[];           // Guid[]
    categoryBudgets: CategoryBudgetDto[];
}
