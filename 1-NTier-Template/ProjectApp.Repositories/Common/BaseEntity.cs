namespace ProjectApp.Repositories;

public abstract class BaseEntity<T>
{
    public T Id { get; set; } = default!;
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.CreateVersion7();
    }
}
