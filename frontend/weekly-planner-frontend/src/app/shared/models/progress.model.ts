// ── GET /api/cycles/{id}/progress ──────────────────────────────────────────
export interface MemberProgressSummary {
    cycleMemberId: string;   // CycleMember.Id (Guid)
    teamMemberId: string;    // TeamMember.Id (Guid)
    name: string;
    isReady: boolean;
    allocatedHours: number;
    plannedHours: number;
    remainingHours: number;
    taskCount: number;
}

export interface CategoryProgressSummary {
    category: string;
    budgetHours: number;
    usedHours: number;
    utilization: number;  // 0-100 %
}

export interface CycleProgress {
    cycleId: string;
    weekStartDate: string;
    status: string;
    totalMembers: number;
    readyMembers: number;
    totalAllocatedHours: number;
    totalPlannedHours: number;
    utilizationPercent: number;
    members: MemberProgressSummary[];
    categoryBreakdown: CategoryProgressSummary[];
}

// ── GET /api/cycles/{id}/members/{cycleMemberId}/progress ──────────────────
export interface MemberTask {
    assignmentId: string;
    backlogItemId: string;
    title: string;
    category: string;
    priority: string;
    plannedHours: number;
}

export interface MemberProgress {
    cycleMemberId: string;
    teamMemberId: string;
    name: string;
    isReady: boolean;
    allocatedHours: number;
    plannedHours: number;
    remainingHours: number;
    tasks: MemberTask[];
}

// ── GET /api/cycles/{id}/category-progress ─────────────────────────────────
export interface CategoryProgress {
    category: string;
    percentage: number;
    budgetHours: number;
    usedHours: number;
    remaining: number;
    utilization: number;
}

// ── GET /api/cycles/{id} — Extended cycle detail ────────────────────────────
// This gives Members[].Id = the CycleMember PK needed for assignments
export interface CycleMemberDetail {
    id: string;          // CycleMember.Id (the PK needed for PUT /member-plans/{id}/ready)
    teamMemberId: string;
    name: string;
    allocatedHours: number;
    isReady: boolean;
}

export interface CycleDetail {
    id: string;
    weekStartDate: string;
    status: string;
    createdAt: string;
    members: CycleMemberDetail[];
    categoryBudgets: { category: string; percentage: number; hoursBudget: number }[];
}
