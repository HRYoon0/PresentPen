using System.Runtime.InteropServices;

namespace PresentPen.Services
{
    /// <summary>
    /// 마우스 속도 조절 (스포트라이트 모드에서 정밀 조작을 위해 속도 감속)
    /// SystemParametersInfo를 사용하여 마우스 속도 변경
    /// </summary>
    public class MouseSpeedController
    {
        private static MouseSpeedController? _instance;
        public static MouseSpeedController Instance => _instance ??= new MouseSpeedController();

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int pvParam, uint fWinIni);

        private const uint SPI_GETMOUSESPEED = 0x0070;
        private const uint SPI_SETMOUSESPEED = 0x0071;

        private int _originalSpeed;
        private bool _isEnabled;

        private MouseSpeedController() { }

        /// <summary>
        /// 마우스 속도 감속 시작
        /// </summary>
        /// <param name="speedLevel">속도 레벨 (1~20, Windows 기본값 10)</param>
        public void Start(int speedLevel = 3)
        {
            if (_isEnabled) return;

            // 원래 속도 저장
            int currentSpeed = 0;
            SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref currentSpeed, 0);
            _originalSpeed = currentSpeed;

            // 속도 감속
            SystemParametersInfo(SPI_SETMOUSESPEED, 0, speedLevel, 0);
            _isEnabled = true;
        }

        /// <summary>
        /// 마우스 속도 원래대로 복원
        /// </summary>
        public void Stop()
        {
            if (!_isEnabled) return;

            SystemParametersInfo(SPI_SETMOUSESPEED, 0, _originalSpeed, 0);
            _isEnabled = false;
        }
    }
}
