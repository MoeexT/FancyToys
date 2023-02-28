using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;

using FancyToys.Logging;


namespace FancyToys.Utils;

/// <summary>
/// Short tcp connection manager.
/// </summary>
public class Messenger {

    public delegate void ServerConnectedHandler();
    public event ServerConnectedHandler OnServerConnected;
    
    
    private TcpClient _client;
    private byte[] _buf;
     public string IP;
    public int Port;

    public Messenger() {}

    public async Task<bool> WriteString(string s) {
        if (!await Check()) {
            return false;
        }

        await _client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(s));

        Close();

        return true;
    }

    public async Task<bool> WriteStorageItems(IReadOnlyList<IStorageItem> list) {
        return await Check();

    }

    public async Task<bool> WriteStream(IRandomAccessStreamWithContentType iStream) {
        if (!await Check()) {
            return false;
        }

        Stream stream = iStream.AsStreamForRead();
        long originalPosition = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        await stream.CopyToAsync(_client.GetStream());
        stream.Seek(originalPosition, SeekOrigin.Begin);
        Close();

        return true;
    }

    public async Task<bool> TestConnection() {
        if (!await Check()) {
            return false;
        }

        if (_client.Connected) {
            return true;
        }

        try {
            await _client.ConnectAsync(IP, Port);
            Dogger.Debug($"Connected to {IP}{Port}");
            await _client.GetStream().WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

            long len = 0;

            while (_client.Connected && _client.Available > 0) {
                int readLen = await _client.GetStream().ReadAsync(_buf.AsMemory(0, 1024));
                len += readLen;
            }

            Dogger.Info($"Read {len} bytes from {IP}:{Port}");
            return true;
        } catch (Exception e) {
            Dogger.Error(e.Message);
            return false;
        }

    }

    private async Task<bool> Check() {
        if (string.IsNullOrEmpty(IP) || !IPAddress.TryParse(IP, out IPAddress passedAddress) || Port is <= 0 or > 65535) {
            Dogger.Error($"Invalid socket address.");
            return false;
        }
        
        _buf ??= new byte[1024];
        _client = new TcpClient();

        if (!_client.Connected) {
            try {
                await _client.ConnectAsync(passedAddress, Port);
            } catch (Exception e) {
                Dogger.Error(e.Message);
                return false;
            }
        }
        OnServerConnected?.Invoke();
        return true;
    }

    private void Close() {
        _client.Close();
    }
}
