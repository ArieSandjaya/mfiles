using System.Net.Http;

namespace MFilesClone.Services;

// Holds the M-Files server base URL and builds HttpClients that flow the current
// Windows logon to the server transparently (Negotiate/Kerberos), so there's no
// separate login UI in the desktop client.
public static class ServerConnection
{
    private const string DefaultServerUrl = "http://localhost:5080";

    public static string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("MFILESCLONE_SERVER_URL") is { Length: > 0 } configured
            ? configured
            : DefaultServerUrl;

    public static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { UseDefaultCredentials = true };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
        };
    }
}
