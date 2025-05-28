namespace TecChallenge.Domain.Entities;

public class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
}