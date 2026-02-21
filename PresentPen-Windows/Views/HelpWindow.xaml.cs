using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentPen.Views
{
    public partial class HelpWindow : Window
    {
        private readonly Button[] _tabs;
        private int _selectedTab;

        public HelpWindow()
        {
            InitializeComponent();
            _tabs = new[] { Tab0, Tab1, Tab2, Tab3, Tab4 };
            ShowTab(0);
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag && int.TryParse(tag, out int tabIndex))
            {
                ShowTab(tabIndex);
            }
        }

        private void ShowTab(int tabIndex)
        {
            _selectedTab = tabIndex;

            // 탭 버튼 스타일
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].Background = i == tabIndex
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
            }

            ContentPanel.Children.Clear();

            switch (tabIndex)
            {
                case 0: ShowShortcutsTab(); break;
                case 1: ShowDrawingTab(); break;
                case 2: ShowZoomTab(); break;
                case 3: ShowCursorSpotlightTab(); break;
                case 4: ShowTimerTab(); break;
            }
        }

        private void ShowShortcutsTab()
        {
            AddSection("모드 전환");
            AddRow("Ctrl + 1", "화면 확대 (줌)");
            AddRow("Ctrl + 2", "그리기 모드");
            AddRow("Ctrl + 3", "타이머");
            AddRow("Ctrl + 4", "스포트라이트");
            AddRow("Ctrl + 5", "커서 하이라이트");
            AddRow("ESC", "모든 모드 종료");

            AddSection("그리기 관련");
            AddRow("Ctrl + Z", "실행 취소");
            AddRow("E", "전체 지우기");

            AddSection("커서 하이라이트");
            AddRow("Ctrl + Shift + 5", "스타일 변경 (링→헤일로→채움→스퀴클)");
            AddRow("Ctrl + Alt + 5", "색상 변경 (9가지 순환)");

            AddSection("스포트라이트");
            AddRow("Ctrl + Shift + 4", "줌 효과 토글");
        }

        private void ShowDrawingTab()
        {
            AddSection("색상 변경");
            AddRow("R", "빨강");
            AddRow("G", "초록");
            AddRow("B", "파랑");
            AddRow("Y", "노랑");
            AddRow("O", "주황");
            AddRow("P", "분홍");

            AddSection("형광펜 모드");
            AddRow("Shift + 색상키", "반투명 형광펜으로 전환");

            AddSection("도구 (마우스 클릭 시 수정자 키)");
            AddRow("Shift + 클릭", "직선");
            AddRow("Ctrl + 클릭", "사각형");
            AddRow("Tab", "원 도구 전환");
            AddRow("Ctrl + Shift + 클릭", "화살표");

            AddSection("배경");
            AddRow("W", "화이트보드 배경 토글");
            AddRow("K", "블랙보드(칠판) 배경 토글");

            AddSection("기타");
            AddRow("마우스 스크롤", "선 굵기 조절");
            AddRow("E", "전체 지우기 + 배경 초기화");
            AddRow("Ctrl + Z", "실행 취소");
        }

        private void ShowZoomTab()
        {
            AddSection("줌 조작");
            AddRow("마우스 스크롤", "확대 / 축소");
            AddRow("마우스 이동", "확대 중심 이동");

            AddSection("줌 모드 내 기능");
            AddRow("Ctrl + 1", "줌 화면 위에 그리기 토글");
            AddRow("Ctrl + 3", "줌 내 커서 하이라이트 토글");
            AddRow("Ctrl + 4", "줌 내 스포트라이트 토글");
            AddRow("Ctrl + Z", "줌 내 그리기 실행 취소");
            AddRow("R/G/B/Y", "줌 내 그리기 색상 변경");
            AddRow("E", "줌 내 그리기 전체 지우기");

            AddInfo("줌 모드에서 다른 기능들을 함께 사용할 수 있습니다.", "#2196F3");
        }

        private void ShowCursorSpotlightTab()
        {
            AddSection("커서 하이라이트");
            AddRow("Ctrl + 5", "켜기 / 끄기");
            AddRow("Ctrl + Shift + 5", "스타일 변경 (4가지 순환)");
            AddRow("Ctrl + Alt + 5", "색상 변경 (9가지 순환)");

            AddSection("스타일 종류");
            AddRow("링 (Ring)", "테두리만 있는 원");
            AddRow("헤일로 (Halo)", "그라데이션 후광 효과");
            AddRow("채움 (Filled)", "반투명하게 채워진 원");
            AddRow("스퀴클 (Squircle)", "둥근 사각형");

            AddSection("스포트라이트");
            AddRow("Ctrl + 4", "켜기 / 끄기");
            AddRow("Ctrl + Shift + 4", "줌(돋보기) 모드 토글");
            AddRow("마우스 스크롤", "스포트라이트 크기 조절");
            AddRow("Shift + 스크롤", "줌 배율 조절 (돋보기 모드)");

            AddInfo("스포트라이트 활성화 시 마우스 속도가 자동으로 느려져\n정밀한 조작이 가능합니다.", "#FF9800");
        }

        private void ShowTimerTab()
        {
            AddSection("시간 설정 (시작 전)");
            AddRow("1", "1분");
            AddRow("3", "3분");
            AddRow("5", "5분");
            AddRow("0", "10분");
            AddRow("↑ / ↓", "1분 단위 증가 / 감소");
            AddRow("마우스 스크롤", "10초 단위 세밀한 조절");

            AddSection("타이머 조작");
            AddRow("Space / Enter", "시작 / 일시정지 / 재개");
            AddRow("ESC", "타이머 종료");

            AddInfo("남은 시간이 1분 미만이면 숫자가 빨간색으로 변경됩니다.\n시간 종료 시 화면이 깜빡이며 알림음이 재생됩니다.", "#F44336");
        }

        // UI 헬퍼
        private void AddSection(string title)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 16, 0, 8)
            });
        }

        private void AddRow(string shortcut, string description)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var keyText = new TextBlock
            {
                Text = shortcut,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4FC3F7")),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            var descText = new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13
            };
            Grid.SetColumn(descText, 1);

            grid.Children.Add(keyText);
            grid.Children.Add(descText);
            ContentPanel.Children.Add(grid);
        }

        private void AddInfo(string text, string colorHex)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x30,
                    ((Color)ColorConverter.ConvertFromString(colorHex)).R,
                    ((Color)ColorConverter.ConvertFromString(colorHex)).G,
                    ((Color)ColorConverter.ConvertFromString(colorHex)).B)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 12, 0, 0)
            };

            border.Child = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };

            ContentPanel.Children.Add(border);
        }
    }
}
