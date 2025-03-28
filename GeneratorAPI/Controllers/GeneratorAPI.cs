using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[Route("api/greeting")]
public class GreetingController : ControllerBase
{
    private static readonly string sdPath = Path.Combine(Directory.GetCurrentDirectory(), "stable-diffusion\\stable-diffusion.cpp\\build\\bin\\Release", "sd.exe");
    private static readonly string ModelPath = Path.Combine(Directory.GetCurrentDirectory(), "stable-diffusion", "stable_diffusion-ema-pruned-v2-1_768.q8_0.gguf");
    private static readonly string OutputImagePath = Path.Combine(Directory.GetCurrentDirectory(), "stable-diffusion\\stable-diffusion.cpp\\build\\bin\\Release", "output.png");

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] UserPrompt request)
    {
        string arguments = $"-m {ModelPath} -p \"{request.Prompt}\" --steps 20 -o \"{OutputImagePath}\"";

        // 打印要執行的命令
        Console.WriteLine($"Executing command: {sdPath} {arguments}");

        if (System.IO.File.Exists(OutputImagePath))
        {
            System.IO.File.Delete(OutputImagePath);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = sdPath,
            Arguments = arguments,
            RedirectStandardOutput = false, // 不捕獲標準輸出
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            // 使用非同步方式啟動，不阻塞主線程
            var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // 返回狀態，表示圖像生成已開始，等待完成
            return Ok(new { message = "Image generation started. Please check back later." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("check-image")]
    public async Task<IActionResult> CheckImage()
    {
        // 每次訪問此端點時檢查圖片是否生成完成
        if (System.IO.File.Exists(OutputImagePath))
        {
            byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(OutputImagePath);
            return File(imageBytes, "image/png"); // 返回圖片的二進制數據
        }

        return NotFound(new { message = "Image not ready yet." });
    }

    public class UserPrompt
    {
        public required string Prompt { get; set; }
    }
}
