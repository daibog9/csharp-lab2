using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;

namespace Core.Storage;

internal sealed class LoopbackCodeReceiver : ICodeReceiver {
    private const string ClosePage =
        "<html><head><meta charset=\"utf-8\"><title>StudentParliament</title></head>" +
        "<body style=\"font-family:system-ui;padding:40px\"><h3>Дозвіл отримано</h3>" +
        "<p>Можна закрити це вікно й повернутися до програми.</p></body></html>";

    private readonly Action<string> _openBrowser;
    private readonly TcpListener _listener;

    public LoopbackCodeReceiver(Action<string> openBrowser) {
        _openBrowser = openBrowser;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        RedirectUri = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/authorize/";
    }

    public string RedirectUri { get; }

    public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken) {
        try {
            _openBrowser(url.Build().ToString());
            return Parse(await ReadRequestAsync(taskCancellationToken));
        } finally {
            _listener.Stop();
        }
    }

    private async Task<string> ReadRequestAsync(CancellationToken token) {
        using TcpClient client = await _listener.AcceptTcpClientAsync(token);
        using NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[8192];
        int read = await stream.ReadAsync(buffer, token);
        if (read == 0) throw new RemoteStorageException("Google не передав код авторизації.");

        byte[] body = Encoding.UTF8.GetBytes(ClosePage);
        byte[] head = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
                                             $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head, token);
        await stream.WriteAsync(body, token);
        await stream.FlushAsync(token);

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static AuthorizationCodeResponseUrl Parse(string request) {
        int start = request.IndexOf('?');
        int end = start < 0 ? -1 : request.IndexOf(' ', start + 1);
        if (start < 0 || end < 0) throw new RemoteStorageException("Google не передав код авторизації.");

        var result = new AuthorizationCodeResponseUrl();
        foreach (string pair in request.Substring(start + 1, end - start - 1).Split('&')) {
            int eq = pair.IndexOf('=');
            if (eq < 0) continue;

            string key = pair[..eq];
            string value = Uri.UnescapeDataString(pair[(eq + 1)..]);

            if (key == "code") result.Code = value;
            else if (key == "state") result.State = value;
            else if (key == "error") result.Error = value;
        }

        if (result.Error is not null) throw new RemoteStorageException($"Google відмовив у доступі ({result.Error}).");
        return result;
    }
}