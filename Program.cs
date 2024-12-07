using System;
using Gtk;

class Program
{
    public static double gameSpeed = 1.0; // 預設速度為 1x

    static void Main(string[] args)
    {
        Application.Init();

        // 創建主視窗
        Window window = new Window("Gameboy 模擬器");
        window.SetDefaultSize(Global.WINDOW_WIDTH * Global.WINDOW_SCALE, Global.WINDOW_HEIGHT * Global.WINDOW_SCALE);
        window.DeleteEvent += (o, e) => Application.Quit();

        // 創建鍵盤處理實例
        Keyboard keyboard = new Keyboard();

        // 創建一個垂直容器
        VBox vbox = new VBox(false, 2);

        // 創建選單
        MenuBar menuBar = new MenuBar();

        // 創建「檔案」選單
        Menu fileMenu = new Menu();
        MenuItem fileMenuItem = new MenuItem("檔案");
        fileMenuItem.Submenu = fileMenu;

        // 在「檔案」選單中添加「開啟檔案」選項
        MenuItem openFileItem = new MenuItem("開啟檔案");
        openFileItem.Activated += (sender, e) =>
        {
            // 顯示檔案選擇對話框
            FileChooserDialog fileChooser = new FileChooserDialog(
                "選擇檔案",
                window,
                FileChooserAction.Open,
                "取消", ResponseType.Cancel,
                "開啟", ResponseType.Accept
            );

            // 添加檔案篩選器
            FileFilter filter = new FileFilter();
            filter.Name = "Gameboy ROM Files (*.gb)";
            filter.AddPattern("*.gb"); // 僅顯示 .gb 檔案
            fileChooser.Filter = filter;

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {

                string filePath = fileChooser.Filename; // 獲取選擇的檔案路徑
                DrawingArea drawingArea = new DrawingArea();
                drawingArea.SetSizeRequest(Global.WINDOW_WIDTH * Global.WINDOW_SCALE, Global.WINDOW_HEIGHT * Global.WINDOW_SCALE);
                drawingArea.CanFocus = true; // 確保 DrawingArea 接受焦點
                drawingArea.FocusOnClick = true;
                drawingArea.GrabFocus();

                // 綁定鍵盤事件到 DrawingArea
                drawingArea.KeyPressEvent += (o, args) =>
                {
                    keyboard.HandleKeyDown((Gdk.EventKey)args.Event);
                };

                drawingArea.KeyReleaseEvent += (o, args) =>
                {
                    keyboard.HandleKeyUp((Gdk.EventKey)args.Event);
                };

                Emulator emulator = new Emulator(filePath, drawingArea, keyboard);

                vbox.PackStart(drawingArea, true, true, 0);
                drawingArea.Show(); // 顯示 DrawingArea
            }
            fileChooser.Destroy(); // 關閉檔案選擇器
        };

        fileMenu.Append(openFileItem);

        // 創建「遊戲速度」選單
        Menu speedMenu = new Menu();
        MenuItem speedMenuItem = new MenuItem("遊戲速度");
        speedMenuItem.Submenu = speedMenu;

        // 遊戲速度選項
        AddSpeedOption(speedMenu, "0.25", 0.25);
        AddSpeedOption(speedMenu, "0.5", 0.5);
        AddSpeedOption(speedMenu, "0.75", 0.75);
        AddSpeedOption(speedMenu, "1", 1.0, true); // 預設為 1
        AddSpeedOption(speedMenu, "1.25", 1.25);
        AddSpeedOption(speedMenu, "1.5", 1.5);
        AddSpeedOption(speedMenu, "1.75", 1.75);
        AddSpeedOption(speedMenu, "2", 2.0);

        menuBar.Append(fileMenuItem);
        menuBar.Append(speedMenuItem);

        // 將選單和其他內容添加到視窗
        vbox.PackStart(menuBar, false, false, 0);
        window.Add(vbox);

        // 確保窗口本身可以接受焦點
        window.KeyPressEvent += (o, args) =>
        {
            keyboard.HandleKeyDown((Gdk.EventKey)args.Event);
        };

        window.KeyReleaseEvent += (o, args) =>
        {
            keyboard.HandleKeyUp((Gdk.EventKey)args.Event);
        };

        window.ShowAll();
        Application.Run();
    }

    private static void AddSpeedOption(Menu speedMenu, string label, double value, bool isDefault = false)
    {
        MenuItem menuItem = new MenuItem(isDefault ? $"✔ {label}" : label);

        menuItem.Activated += (sender, e) =>
        {
            // 更新所有選單項的標籤
            foreach (MenuItem item in speedMenu)
            {
                item.Label = item.Label.Replace("V ", "");
            }

            // 為當前選項添加 "V "
            menuItem.Label = $"V {label}";

            // 更新遊戲速度
            gameSpeed = value;

            // 在終端輸出當前選擇的速度
            Console.WriteLine($"遊戲速度已設置為 {value}x");
        };

        speedMenu.Append(menuItem);
    }
    
}

