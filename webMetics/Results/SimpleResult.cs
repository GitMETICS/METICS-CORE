namespace webMetics.Results;

public class SimpleResult<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public required T Data { get; set; }

    public static SimpleResult<T> Success(string message = "", T data = default!)
    {
        return new SimpleResult<T> { IsSuccess = true, Message = message, Data = data };
    }

    public static SimpleResult<T> Error(string message, T data = default!)
    {
        return new SimpleResult<T> { IsSuccess = false, Message = message, Data = data };
    }
}

public static class SimpleResultExtensions
{
    public static SimpleResult<T> ToSimpleResult<T>(this T value, string notFoundMessage = "Elemento no encontrado")
        where T : class
    {
        return value != null
            ? SimpleResult<T>.Success(data: value)
            : SimpleResult<T>.Error(notFoundMessage);
    }
}
