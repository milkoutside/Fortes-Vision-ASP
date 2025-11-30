namespace Projects.Api.Contracts.Batches;

public class CreateBatchRequest
{
    public string Name { get; set; } = null!;
    public long ProjectId { get; set; }
}

public class UpdateBatchRequest
{
    public string? Name { get; set; }
    public long? ProjectId { get; set; }
}


