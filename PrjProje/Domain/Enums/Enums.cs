namespace PrjProje.Domain.Enums;

public enum QuoteStatus
{
    New = 0,
    InReview = 1,
    QuoteSent = 2,
    Completed = 3
}

public enum TicketType
{
    Bug = 0,
    Request = 1
}

public enum TicketPriority
{
    Low = 0,
    Normal = 1,
    Urgent = 2
}

public enum TicketStatus
{
    New = 0,
    InReview = 1,
    Resolved = 2,
    Rejected = 3
}

public enum PublishStatus
{
    Draft = 0,
    Published = 1
}

public enum AnnouncementDisplay
{
    Bar = 0,
    Popup = 1
}

public enum DocumentType
{
    Certificate = 0,
    QualityDocument = 1,
    Standard = 2,
    Authorization = 3,
    Catalog = 4
}
