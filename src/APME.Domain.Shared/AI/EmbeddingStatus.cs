namespace APME.AI;

public enum EmbeddingStatus
{
    NotEmbedded = 0,
    Queued = 1,
    Processing = 2,
    Current = 3,
    Stale = 4,
    Failed = 5
}
