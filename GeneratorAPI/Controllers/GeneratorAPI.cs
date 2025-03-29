using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[Route("api/greeting")]
public class GreetingController : ControllerBase
{
    // 請自行更換模型路徑
    private static readonly string sdPath = Path.Combine(Directory.GetCurrentDirectory(), "stable-diffusion\\stable-diffusion.cpp\\build\\bin\\Release", "sd.exe");
    private static readonly string ModelPath = Path.Combine(Directory.GetCurrentDirectory(), "stable-diffusion", "stable_diffusion-ema-pruned-v2-1_768.q8_0.gguf");
    private static readonly string OutputImagePath = Path.Combine(Directory.GetCurrentDirectory(), "stable-diffusion\\stable-diffusion.cpp\\build\\bin\\Release", "output.png");

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] UserPrompt request)
    {
        // --steps 圖片精緻度，數值越大生成時間越長，圖片品質越高 (default 20)
        string arguments = $"-m {ModelPath} -p \"{request.Prompt}\" --steps 20 -o \"{OutputImagePath}\"";
        Console.WriteLine($"Executing command: {sdPath} {arguments}"); // debug用

        if (System.IO.File.Exists(OutputImagePath))
        {
            System.IO.File.Delete(OutputImagePath);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = sdPath,
            Arguments = arguments,
            RedirectStandardOutput = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            // 圖片生成較久會導致API請求超時，透過 Task.Run 使用非同步啟動進程，避免阻塞主線程
            await Task.Run(() =>
            {
                var process = new Process { StartInfo = processStartInfo };
                process.Start();
            });

            // 返回狀態，表示圖像生成已開始，等待完成
            return Ok(new { message = "正在生成圖像..." });
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

        return NotFound(new { message = "圖片還未生成" });
    }

    public class UserPrompt
    {
        public required string Prompt { get; set; }
    }
}
