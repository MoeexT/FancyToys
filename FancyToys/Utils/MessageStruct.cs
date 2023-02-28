using System.Runtime.InteropServices;

using Windows.Foundation;


namespace FancyToys.Utils {

    public enum MessageType {
        Control = 0,
        Clip = 1,
    }
    
    public enum EncryptType {
        NoEncrypt,
        AES256,
        RSA,
        DSA,
    }
    
    
    public struct MessageStruct {
        public byte[] Offsets;
        public string Uid;
        public string Token;
        public MessageType Type;
        public EncryptType Encryption;
        public byte[] Content;
    }

}


