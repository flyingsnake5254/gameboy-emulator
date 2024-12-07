using Cairo;
using Gtk;

public class Program
{
    public static void Main(string[] args)
    {
        ShowWindow();
    }

    public static void ShowWindow()
    {
        Application.Init();

        // 視窗
        Window window = new Window("Gameboy 模擬器");
        window.SetDefaultSize(Global.SCREEN_WIDTH * Global.SCREEN_SCALE, Global.SCREEN_HEIGHT * Global.SCREEN_SCALE);
        window.DeleteEvent += (o, e) => Application.Quit();
        window.SetPosition(WindowPosition.Center);

        // 鍵盤
        Keyboard keyboard = new Keyboard();

        // 垂直容器
        VBox vBox = new VBox(false, 2);

        // 上方選單列
        MenuBar menuBar = new MenuBar();

        /*
            選項一：
                檔案
                    |_ 開啟檔案
        */
        MenuItem fileMenuItem = new MenuItem("檔案");
        Menu fileMenuItemSub = new Menu();
        fileMenuItem.Submenu = fileMenuItemSub;

        // 開啟檔案
        MenuItem openFileMenuItem = new MenuItem("開啟檔案");
        openFileMenuItem.Activated += (sender, e) =>
        {
            FileChooserDialog fileChooser = new FileChooserDialog(
                "選擇 gb 檔案",
                window,
                FileChooserAction.Open,
                "取消", ResponseType.Cancel,
                "開啟", ResponseType.Accept
            );

            // 只顯示 .gb 檔案
            FileFilter fileFilter = new FileFilter();
            fileFilter.Name = "Gameboy ROM Files (*.gb)";
            fileFilter.AddPattern("*.gb");
            fileChooser.Filter = fileFilter;

            // 選擇完檔案
            if (fileChooser.Run() == (int) ResponseType.Accept)
            {
                if (vBox.Children.Length > 1) // 假設只有一個 MenuBar，其他是 DrawingArea
                {
                    vBox.Remove(vBox.Children[1]); // 移除舊的遊戲畫布
                }

                // 建立遊戲區塊
                DrawingArea drawingArea = new DrawingArea();
                drawingArea.SetSizeRequest(Global.SCREEN_WIDTH * Global.SCREEN_SCALE, Global.SCREEN_HEIGHT * Global.SCREEN_SCALE);
                drawingArea.CanFocus = true;
                drawingArea.FocusOnClick = true;
                drawingArea.GrabFocus();

                // 綁定 Keyboard 事件
                drawingArea.KeyPressEvent += (o, args) =>
                {
                    keyboard.HandleKeyDown((Gdk.EventKey)args.Event);
                };

                drawingArea.KeyReleaseEvent += (o, args) =>
                {
                    keyboard.HandleKeyUp((Gdk.EventKey)args.Event);
                };

                vBox.PackStart(drawingArea, true, true, 0);
                drawingArea.Show();
                string gbFileName = fileChooser.Filename;
                fileChooser.Destroy();
                Emulator emulator = new Emulator(gbFileName, drawingArea, keyboard);
            }
            else
            {
                fileChooser.Destroy();
            }
            
        };

        fileMenuItemSub.Append(openFileMenuItem);

        /*
            選項二：
                遊戲速度
                    |_ 0.25
                    |_ 0.5
                    |_ 0.75
                    |_ 1.0 (default)
                    |_ 1.25
                    |_ 1.5
                    |_ 1.75
                    |_ 2.0
        */
        MenuItem speedMenuItem = new MenuItem("遊戲速度");
        Menu speedMenuItemSub = new Menu();
        speedMenuItem.Submenu = speedMenuItemSub;

        // 遊戲速度選項
        AddSpeedOption(speedMenuItemSub, "0.25", 0.25);
        AddSpeedOption(speedMenuItemSub, "0.5", 0.5);
        AddSpeedOption(speedMenuItemSub, "0.75", 0.75);
        AddSpeedOption(speedMenuItemSub, "1", 1.0, true); // 預設為 1
        AddSpeedOption(speedMenuItemSub, "1.25", 1.25);
        AddSpeedOption(speedMenuItemSub, "1.5", 1.5);
        AddSpeedOption(speedMenuItemSub, "1.75", 1.75);
        AddSpeedOption(speedMenuItemSub, "2", 2.0);
        
        
        // 添加到 Menubar
        menuBar.Append(fileMenuItem);
        menuBar.Append(speedMenuItem);


        // 添加到 vbox
        vBox.PackStart(menuBar, false, false, 0);

        // 添加到 window
        window.Add(vBox);

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
                item.Label = item.Label.Replace("✔ ", "");
            }

            // 為當前選項添加 "V "
            menuItem.Label = $"✔ {label}";

            // 更新遊戲速度
            Global.GAME_SPEED = value;

            // 在終端輸出當前選擇的速度
            Console.WriteLine($"遊戲速度已設置為 {value}x");
        };

        speedMenu.Append(menuItem);
    }
}