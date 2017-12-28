using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace WTCWallet
{
    public class PublicQRCodeVM
    {
        private BaseCommand _copyPublicKeyCommand;
        public string PublicKey { get; }

        public PublicQRCodeVM(string publicKey)
        {
            PublicKey = publicKey;
            Generate();
        }

        public static BitmapImage GetBitmap(string key)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(key, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);
            using (var memoryStream = new MemoryStream())
            {
                qrCodeImage.Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = memoryStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
               return bitmap;
            }
        }

        public void Generate()
        {
            PublicQRCode = GetBitmap(PublicKey);
        }

        public BaseCommand CopyPublicKeyCommand
        {
            get { return _copyPublicKeyCommand ?? (_copyPublicKeyCommand = new BaseCommand(CopyPublicKey)); }
        }

        private void CopyPublicKey(object obj)
        {
            Clipboard.SetText(PublicKey, TextDataFormat.Text);
        }

        public BitmapImage PublicQRCode { get; set; }
    }
}