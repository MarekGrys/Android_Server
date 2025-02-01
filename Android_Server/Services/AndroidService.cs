//using System.Reflection.Metadata;
using System.Xml.Linq;
using Android_Server.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Android_Server.Services
{
    public class AndroidService : IAndroidService
    {
        private static readonly List<byte[]> photoList = new List<byte[]>();
        private readonly string outputFolder = @"G:\Nowy folder\AndroidPDFy";
        public async Task UploadPhoto(string photo)
        {
            if (string.IsNullOrEmpty(photo))
            {
                throw new ArgumentException("No photo");
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
                throw new ArgumentException("Incorrect data!");
            }

            photoList.Add(bytes);
        }

        public string GenerateDocument()
        {
            if (photoList.Count == 0)
            {
                throw new ArgumentException("No photos!");
            }


            
            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, "test1.pdf");

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
                            Console.WriteLine("Error!!!");
                        }
                    }
                    photoList.Clear();
                }
                document.Close();
            }
            return outputPath;

        }

        public List<FileDetail> GetFiles()
        {
            if (!Directory.Exists(outputFolder))
            {
                throw new DirectoryNotFoundException($"{outputFolder} does not exist.");
            }

            var files = Directory.GetFiles(outputFolder)
                .Select(filePath => new FileDetail
                {
                    FileName = Path.GetFileName(filePath),
                    FullPath = filePath,
                    CreatedDate = File.GetCreationTime(filePath)
                })
                .ToList();

            return files;
        }

        public async Task<PdfFileModel> SendPDF(string fileName)
        {
            var filePath = Path.Combine(outputFolder, fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"PDF file {filePath} not found");
            }

            byte[] fileContent = File.ReadAllBytes(filePath);

            return new PdfFileModel
            {
                FileName = fileName,
                FileContent = fileContent
            };

        }
    }
}
