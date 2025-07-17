using Microsoft.CodeAnalysis.CSharp.Syntax;
using webMetics.Results;

namespace webMetics.Helpers;

public static class SimpleOperationHelper
{
    public static SimpleResult<T> Execute<T>(Func<T> operation, string successMessage = "", string errorMessage = "")
    {
        try
        {
            var result = operation();
            return SimpleResult<T>.Success(successMessage, result);
        }
        catch (Exception ex)
        {
            return SimpleResult<T>.Error(errorMessage ?? ex.Message);
        }
    }
}
