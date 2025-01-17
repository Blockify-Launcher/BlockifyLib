using System.Net.Http;
using System.Text.Json;

namespace BlockifyLib.Launcher.Microsoft.Auth;

internal static class ExceptionHelper
{
    public static Exception CreateException(Exception ex, string resBody, HttpResponseMessage res)
    {
        if (ex is JsonException || ex is HttpRequestException)
        {
            try
            {
                return JEAuthException.FromResponseBody(resBody, (int)res.StatusCode);
            }
            catch (FormatException)
            {
                return new JEAuthException($"{(int)res.StatusCode}: {res.ReasonPhrase}");
            }
        }
        else
            return ex;
    }
}