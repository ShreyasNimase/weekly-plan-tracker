namespace WeeklyPlanner.Core.DTOs;

public class SetupCycleRequest
{
    public DateTime PlanningDate { get; set; }
    public List<Guid> MemberIds { get; set; } = [];
    public List<CategoryAllocationInputDto> CategoryAllocations { get; set; } = [];
}

public class CategoryAllocationInputDto
{
    public string Category { get; set; } = string.Empty;
    public int Percentage { get; set; }
}
