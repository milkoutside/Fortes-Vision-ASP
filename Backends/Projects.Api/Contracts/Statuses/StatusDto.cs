namespace Projects.Api.Contracts.Statuses;

public record StatusDto(
    long Id,
    string Name,
    string Color,
    string TextColor
);


