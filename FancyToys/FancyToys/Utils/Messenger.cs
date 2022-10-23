using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Windows.Storage;
using Windows.Storage.Streams;

using ABI.Windows.Networking.Vpn;

using FancyToys.Logging;


namespace FancyToys.Utils; 

public class Messenger {
    private readonly TcpClient _client;
    private byte[] _buf;
    public Messenger() {
        _buf = new byte[1024];
        _client = new TcpClient("43.139.72.27", 7878);
    }

    public void WriteString(string s) {
        if (!_client.Connected) {
            _client.Connect("43.139.72.27", 7878);
        }
        
        _client.GetStream().Write(Encoding.UTF8.GetBytes(s));
        
        Close();
    }

    public void WriteStorageItems(IReadOnlyList<IStorageItem> list) {
        
    }
    
    public void WriteStream(IRandomAccessStreamWithContentType iStream) {
        if (!_client.Connected) {
            _client.Connect("43.139.72.27", 7878);
        }

        Stream stream = iStream.AsStreamForRead();
        long originalPosition = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(_client.GetStream());
        stream.Seek(originalPosition, SeekOrigin.Begin);
        Close();
    }

    private void Close() {
        _client.Close();
    }
}
