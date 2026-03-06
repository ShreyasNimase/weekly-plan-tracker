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

// New: matches backend CategoryAllocationItemDto
export interface CategoryAllocationItem {
    category: string;       // CLIENT_FOCUSED | TECH_DEBT | R_AND_D
    percentage: number;
    budgetHours: number;
}

// New: matches backend MemberPlanSummaryDto
export interface MemberPlanSummary {
    id: string;
    memberId: string;
    isReady: boolean;
    totalPlannedHours: number;
}

export interface Cycle {
    id: string;

    // ── Backend CycleDto fields (returned by /api/cycles/active) ──────────────
    state: string;                          // SETUP | PLANNING | FROZEN | COMPLETED | CANCELLED
    planningDate?: string;                  // "yyyy-MM-dd" (Tuesday)
    executionStartDate?: string;            // Wednesday after planning date
    executionEndDate?: string;              // Monday after planning date
    teamCapacity?: number;                  // members × 30
    participatingMemberIds?: string[];      // Guids of members in this cycle
    categoryAllocations?: CategoryAllocationItem[];
    memberPlans?: MemberPlanSummary[];

    // ── Old / compat fields ───────────────────────────────────────────────────
    weekStartDate?: string;                 // "yyyy-MM-dd" – kept for old code
    status?: string;                        // Pascal: Setup | Planning | Frozen | Completed
    createdAt?: string;
    members?: CycleMember[];               // old member list shape
    categoryBudgets?: CategoryBudget[];    // old allocation shape
}

// POST /api/cycles/start
export interface StartCycleDto {
    weekStartDate: string;  // ISO date string, must be a Tuesday
}

// CategoryBudgetDto used inside SetupCycleDto
export interface CategoryBudgetDto {
    category: string;
    percentage: number;
}

// PUT /api/cycles/{id}/setup
export interface SetupCycleDto {
    planningDate: string;            // ISO date string YYYY-MM-DD, must be a Tuesday
    memberIds: string[];             // Guid[]
    categoryAllocations?: CategoryBudgetDto[];  // preferred backend field name
    categoryBudgets?: CategoryBudgetDto[];      // kept for backward compat
}
