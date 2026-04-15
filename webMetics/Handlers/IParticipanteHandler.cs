namespace webMetics.Handlers;

/// <summary>
/// Exposes the read-only, non-DB operations of ParticipanteHandler needed for
/// unit testing controller logic without a live database connection.
/// Extend this interface as more methods are covered by tests.
/// </summary>
public interface IParticipanteHandler
{
    List<string> GetAllAreas();
}
