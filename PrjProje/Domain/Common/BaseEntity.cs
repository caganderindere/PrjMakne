namespace PrjProje.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}

public interface ITranslation
{
    string LanguageCode { get; set; }
}

public interface ITranslatable<TTranslation> where TTranslation : ITranslation
{
    ICollection<TTranslation> Translations { get; set; }
}
