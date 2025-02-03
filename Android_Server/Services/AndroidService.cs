//using System.Reflection.Metadata;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;
using Android_Server.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

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

        public async Task<string> GenerateDocument(string name, string language)
        {
            if (photoList.Count == 0)
            {
                throw new ArgumentException("No photos!");
            }

            // Utwórz lokalną kopię zdjęć i wyczyść oryginalną listę w bloku lock
            List<byte[]> photos;
            lock (photoList)
            {
                photos = new List<byte[]>(photoList);
                photoList.Clear();
            }

            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, $"{name}.pdf");

            using (FileStream stream = new FileStream(outputPath, FileMode.Create))
            {
                Document document = new Document();
                PdfWriter.GetInstance(document, stream);
                document.Open();

                foreach (var photo in photos)
                {
                    try
                    {
                        iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(photo);
                        img.ScaleToFit(document.PageSize.Width - 20, document.PageSize.Height - 20);
                        img.Alignment = Element.ALIGN_CENTER;
                        document.Add(img);

                        // Konwersja zdjęcia na Base64
                        string base64Photo = Convert.ToBase64String(photo);

                        // Używamy await poza blokiem lock
                        string apiDescription = await GetPhotoDescription(base64Photo, language);

                        var paragraph = new iTextSharp.text.Paragraph(apiDescription);
                        paragraph.Alignment = Element.ALIGN_CENTER;
                        document.Add(paragraph);
                        document.NewPage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
                var date = DateTime.Now.ToString("dd/MM/yyyy");
                var hour = DateTime.Now.Hour.ToString();
                var minute = DateTime.Now.Minute.ToString();
                var endParagraph = new iTextSharp.text.Paragraph("Data utworzenia: " + date + "," + hour + ":" + minute);
                endParagraph.Alignment = Element.ALIGN_CENTER;
                document.Add(endParagraph);
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

        // Klucz API z https://aistudio.google.com/apikey
        private static readonly string API_KEY = "AIzaSyBFLpxjbaEMkof6ltIUv3hK7xQWHjTp40I";
        private static readonly string API_URL = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={API_KEY}";

        public async Task<string> GetPhotoDescription(string base64Photo, string language)
        {
            string prompt = "";

            if(language == "Polski")
            {
                prompt = "Podaj mi krótki opis kto jest na zdjęciu lub co na nim widzisz";
            }
            if(language == "English")
            {
                prompt = "Give me a short description who is at the image or what you can find on that image";
            }
            
            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            // Prompt do Gemini, zależnie co chcemy, żeby powiedział o tym zdjęciu
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Photo
                                }
                            }
                        }
                    }
                }
            };


            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(API_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API request failed: {response.StatusCode}");
            }

            string resultJson = await response.Content.ReadAsStringAsync();
            JsonDocument json = JsonDocument.Parse(resultJson);

            // Extract AI-generated description
            string description = json.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return description ?? "No description found.";
        }

        public async Task<string> GetMultiplePhotosDescription(string[] base64Photos)
        {
            var parts = new List<object>
            {
                new { text = "Give me one description of who is in the images or what can be found in them." }
            };

            foreach (var base64Photo in base64Photos)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = "image/jpeg",
                        data = base64Photo
                    }
                });
            }

            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = parts
                    }
                }
            };

            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(API_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API request failed: {response.StatusCode}");
            }

            string resultJson = await response.Content.ReadAsStringAsync();
            JsonDocument json = JsonDocument.Parse(resultJson);

            string description = json.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return description ?? "No description found.";
        }
    }
}
