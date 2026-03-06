export enum CycleStatus {
    Setup = 'Setup',
    Planning = 'Planning',
    Frozen = 'Frozen',
    Completed = 'Completed',
    Cancelled = 'Cancelled',
}

export enum BacklogStatus {
    Active = 'Active',
    Archived = 'Archived',
}

export enum BacklogCategory {
    Feature = 'Feature',
    Bug = 'Bug',
    TechDebt = 'TechDebt',
    Learning = 'Learning',
    Other = 'Other',
}

export enum BacklogPriority {
    Low = 'Low',
    Medium = 'Medium',
    High = 'High',
}
