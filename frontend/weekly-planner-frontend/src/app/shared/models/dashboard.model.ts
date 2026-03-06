// ── GET /api/dashboard ──────────────────────────────────────────────────────
export interface ActiveCycleSnapshot {
    id: string;
    weekStartDate: string;
    status: string;
    totalMembers: number;
    readyMembers: number;
    totalAllocatedHours: number;
    totalPlannedHours: number;
    taskCount: number;
}

export interface TeamSummary {
    total: number;
    active: number;
    inactive: number;
    leadName: string;
}

export interface BacklogPrioritySummary {
    high: number;
    medium: number;
    low: number;
}

export interface BacklogSummary {
    total: number;
    active: number;
    archived: number;
    byPriority: BacklogPrioritySummary;
}

export interface RecentCycle {
    id: string;
    weekStartDate: string;
    status: string;
    memberCount: number;
}

export interface DashboardData {
    activeCycle: ActiveCycleSnapshot | null;
    team: TeamSummary;
    backlog: BacklogSummary;
    recentHistory: RecentCycle[];
}
