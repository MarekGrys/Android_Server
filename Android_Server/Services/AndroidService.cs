//using System.Reflection.Metadata;
using System.Xml.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Android_Server.Services
{
    public class AndroidService : IAndroidService
    {
        private static readonly List<byte[]> photoList = new List<byte[]>();

        public async Task UploadPhoto(string photo)
        {
            if (string.IsNullOrEmpty(photo))
            {
                throw new ArgumentException("Brak zdjęcia");
            }

            byte[] bytes;

            try
            {
                var base64Data = photo;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                }

                bytes = Convert.FromBase64String(base64Data);
            }
            catch
            {
                throw new ArgumentException("Źle podane dane!");
            }

            photoList.Add(bytes);
        }

        public string GenerateDocument()
        {
            if (photoList.Count == 0)
            {
                throw new ArgumentException("Brak zdjęć!");
            }


            var outputFolder = @"G:\Nowy folder\AndroidPDFy";
            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, "test.pdf");

            using (FileStream stream = new FileStream(outputPath, FileMode.Create))
            {
                Document document = new Document();
                PdfWriter.GetInstance(document, stream);
                document.Open();


                lock (photoList)
                {
                    foreach (var photo in photoList)
                    {
                        try
                        {
                            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(photo);
                            img.ScaleToFit(document.PageSize.Width - 20, document.PageSize.Height - 20);
                            img.Alignment = Element.ALIGN_CENTER;
                            document.Add(img);
                            document.NewPage();
                        }
                        catch
                        {
                            Console.WriteLine("Błąd!!!");
                        }
                    }
                    photoList.Clear();
                }
                document.Close();
            }
            return outputPath;

        }
    }
}
