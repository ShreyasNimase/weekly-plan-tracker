import { BacklogCategory, BacklogPriority, BacklogStatus } from '../enums/status.enum';

export interface BacklogItem {
    id: string;         // Guid
    title: string;
    description?: string;
    category: string;   // returned as string from backend
    status: string;     // returned as string from backend
    priority: string;   // returned as string from backend
    estimatedHours?: number;
    createdAt: string;
}

export interface CreateBacklogItemDto {
    title: string;
    description?: string;
    category: BacklogCategory;
    priority: BacklogPriority;
    estimatedHours?: number;
}

export interface UpdateBacklogItemDto {
    title: string;
    description?: string;
    category: BacklogCategory;
    priority: BacklogPriority;
    estimatedHours?: number;
}

export interface BacklogFilterParams {
    category?: BacklogCategory;
    status?: BacklogStatus;
    search?: string;
}
