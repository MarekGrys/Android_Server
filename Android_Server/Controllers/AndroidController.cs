using Android_Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Android_Server.Controllers
{
    [ApiController]
    [Route("api/android")]
    public class AndroidController : ControllerBase
    {
        private readonly IAndroidService _service;

        public AndroidController(IAndroidService service)
        {
            _service = service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto([FromBody] string photo)
        {
            try
            {
                await _service.UploadPhoto(photo);
                return Ok(new { message = "Photo uploaded" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate")]
        public IActionResult GenerateDocument()
        {
            try
            {
                var pdfPath = _service.GenerateDocument();
                return Ok(new { message = "Document created", filePath = pdfPath });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("files")]
        public IActionResult GetFiles()
        {
            try
            {
                var files = _service.GetFiles();
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500,$"An error occured:{ex.Message}");
            }
        }

        [HttpGet("file")]
        public async Task<IActionResult> SendPDF([FromQuery] string fileName)
        {
            try
            {
                var pdf = await _service.SendPDF(fileName);
                return File(pdf.FileContent, "application/pdf", pdf.FileName);
            }
            catch(FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"An error occured: {ex.Message}");
            }
        }
    }
}
