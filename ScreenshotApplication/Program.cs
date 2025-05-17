using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

class Program
{
    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    const int SRCCOPY = 0x00CC0020;

    static async Task Main()
    {
        int width = GetSystemMetrics(0); // SM_CXSCREEN
        int height = GetSystemMetrics(1); // SM_CYSCREEN
        string screenshotsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Screenshots");
        Directory.CreateDirectory(screenshotsDir);
        using var httpClient = new HttpClient();
        while (true)
        {
            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);

            IntPtr desktopWnd = GetDesktopWindow();
            IntPtr desktopDC = GetWindowDC(desktopWnd);
            IntPtr bmpDC = g.GetHdc();

            BitBlt(bmpDC, 0, 0, width, height, desktopDC, 0, 0, SRCCOPY);

            g.ReleaseHdc(bmpDC);
            ReleaseDC(desktopWnd, desktopDC);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"screenshot_{timestamp}.png";
            var path = Path.Combine(screenshotsDir, filename);
            bmp.Save(path, ImageFormat.Png);

            Console.WriteLine($"Screenshot saved to: {path}");

            // Upload to remote server (example: http://127.0.0.1:5000/upload)
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(ms), "file", filename);
            try
            {
                var response = await httpClient.PostAsync("http://127.0.0.1:5000/upload", content);
                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"Uploaded to remote server: {response.StatusCode}");
                else
                    Console.WriteLine($"Failed to upload: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
            }

            await Task.Delay(1000);
        }
    }

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
}