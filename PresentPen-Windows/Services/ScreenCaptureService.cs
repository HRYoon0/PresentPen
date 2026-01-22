using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PresentPen.Services
{
    /// <summary>
    /// 화면 캡처 서비스
    /// 줌 기능을 위한 스크린샷 캡처
    /// </summary>
    public static class ScreenCaptureService
    {
        /// <summary>
        /// 전체 화면 캡처
        /// </summary>
        public static BitmapSource CaptureScreen()
        {
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            using var bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));

            return ConvertToBitmapSource(bitmap);
        }

        /// <summary>
        /// 특정 영역 캡처
        /// </summary>
        public static BitmapSource CaptureRegion(int x, int y, int width, int height)
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

            return ConvertToBitmapSource(bitmap);
        }

        /// <summary>
        /// 마우스 위치 중심으로 캡처 (줌용)
        /// </summary>
        public static BitmapSource CaptureAroundCursor(int radius)
        {
            var cursorPos = GetCursorPosition();
            int x = Math.Max(0, cursorPos.X - radius);
            int y = Math.Max(0, cursorPos.Y - radius);
            int width = radius * 2;
            int height = radius * 2;

            // 화면 경계 체크
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            if (x + width > screenWidth) width = screenWidth - x;
            if (y + height > screenHeight) height = screenHeight - y;

            return CaptureRegion(x, y, width, height);
        }

        /// <summary>
        /// System.Drawing.Bitmap을 WPF BitmapSource로 변환
        /// </summary>
        private static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        /// <summary>
        /// 현재 마우스 커서 위치 가져오기
        /// </summary>
        public static System.Drawing.Point GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return new System.Drawing.Point(point.X, point.Y);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
