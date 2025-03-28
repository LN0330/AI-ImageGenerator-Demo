using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void GenerateImage(object sender, RoutedEventArgs e)
        {
            string userInput = UserInput.Text;

            if (string.IsNullOrWhiteSpace(userInput))
            {
                MessageBox.Show("請輸入內容！");
                return;
            }

            var requestBody = new { Prompt = userInput };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            AiResponse.Text = "正在生成圖像...";

            try
            {
                var response = await _httpClient.PostAsync("http://localhost:5093/api/greeting/generate-image", content);
                if (response.IsSuccessStatusCode)
                {
                    AiResponse.Text = "圖像生成中，請稍候...";
                    StartCheckingForImage();
                }
                else
                {
                    AiResponse.Text = "圖像生成請求失敗";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("發生錯誤：" + ex.Message);
                AiResponse.Text = "無法獲得回應";
            }
        }

        private void StartCheckingForImage()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CheckForImageAsync(_cancellationTokenSource.Token));
        }

        private async Task CheckForImageAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.GetAsync("http://localhost:5093/api/greeting/check-image");

                    if (response.IsSuccessStatusCode)
                    {
                        var imageBytes = await response.Content.ReadAsByteArrayAsync();
                        Console.WriteLine("Received Image Bytes: " + imageBytes.Length);

                        // 顯示圖片
                        Dispatcher.Invoke(() =>
                        {
                            AiResponse.Text = "圖像生成成功！";
                            DisplayImage(imageBytes);
                        });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Dispatcher.Invoke(() => AiResponse.Text = "發生錯誤：" + ex.Message);
                }

                await Task.Delay(5000, token); // 每 5 秒檢查一次
            }
        }

        private void DisplayImage(byte[] imageBytes)
        {
            try
            {
                using (var stream = new System.IO.MemoryStream(imageBytes))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    AiImage.Source = bitmap;  // 顯示圖像
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AiResponse.Text = "無法顯示圖片：" + ex.Message);
                Console.WriteLine("Error displaying image: " + ex.Message);
            }
        }
    }
}
