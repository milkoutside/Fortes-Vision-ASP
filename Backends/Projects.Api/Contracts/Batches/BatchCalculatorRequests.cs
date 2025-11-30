namespace Projects.Api.Contracts.Batches;

public class StatusDurationDto
{
    public long Status { get; set; }
    public int Duration { get; set; }
}

public class CreateBatchCalculatorRequest
{
    public long BatchId { get; set; }
    public long ProjectId { get; set; }
    public List<StatusDurationDto> StatusDurations { get; set; } = new();
}

