export enum CycleStatus {
    Setup = 'Setup',
    Planning = 'Planning',
    Frozen = 'Frozen',
    Completed = 'Completed',
    Cancelled = 'Cancelled',
}

// Backend returns UPPER_CASE for backlog item status
export enum BacklogStatus {
    Available = 'AVAILABLE',    // item is open to be picked
    InPlan = 'IN_PLAN',      // item is assigned in the current cycle
    Completed = 'COMPLETED',    // item was completed
    Archived = 'Archived',     // legacy / soft-deleted
}

// Backend expects EXACTLY these three strings
export enum BacklogCategory {
    ClientFocused = 'CLIENT_FOCUSED',
    TechDebt = 'TECH_DEBT',
    RAndD = 'R_AND_D',
}

export enum BacklogPriority {
    Low = 'Low',
    Medium = 'Medium',
    High = 'High',
}
