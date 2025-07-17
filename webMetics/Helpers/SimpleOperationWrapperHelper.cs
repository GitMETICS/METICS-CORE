using webMetics.Results;

namespace webMetics.Helpers;

public static class SimpleOperationWrapperHelper
{
    public static SimpleResult<T> ExecuteWithNullCheck<T>(Func<T> operation, string? notFoundMessage = null, string? errorMessage = null)
        where T : class
    {
        try
        {
            var result = operation();
            return result != null
                ? SimpleResult<T>.Success(data: result)
                : SimpleResult<T>.Error(notFoundMessage ?? "Elemento no encontrado");
        }
        catch (Exception ex)
        {
            return SimpleResult<T>.Error(errorMessage ?? ex.Message);
        }
    }
}
