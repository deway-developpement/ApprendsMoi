namespace backend.Domains.Subjects;

public class SubjectDto {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class CreateSubjectDto {
    public string Name { get; set; } = string.Empty;
}

public class UpdateSubjectDto {
    public string? Name { get; set; }
}
