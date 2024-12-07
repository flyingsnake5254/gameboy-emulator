using Gtk;
using Cairo;
using System.Runtime.CompilerServices;

public class PPU {

    private int[] _color = new int[] { 0x00FFFFFF, 0x00808080, 0x00404040, 0 }; // 調色板顏色
    private DrawingArea _drawingArea;
    private int[,] _frameBuffer = new int[Global.SCREEN_WIDTH, Global.SCREEN_HEIGHT];
    private int _countCycles;
    private MMU _mmu;

    public PPU(ref MMU mmu, DrawingArea drawingArea) {
        this._mmu = mmu;
        _drawingArea = drawingArea;
        _drawingArea.Drawn += OnDrawn;
    }

    /*
        每條 Scan Line 歷程：
        Mode     | STAT (1-0 bit)| cycles| Description]
        ---------+---------------+-------+-----------------
        HBlank   |      00       |  204  | 水平空白期間，LCD 停止渲染，CPU 可以訪問 VRAM/OAM
        VBlank   |      01       |  456  | 垂直空白期間，LCD 不再繪製掃描線，CPU 可進行畫面更新。 
        OAM 搜索  |      10       |  80   | 搜索 OAM 以準備渲染精靈（Sprite）
        VRAM 傳輸 |      11       |  172  | 從 VRAM 中提取圖塊數據並進行渲染 

        PPU 更新流程
        - LCD (LCDC bit 7) 是否啟用：
            1 (啟用) ：運行 PPU 更新
            0 (禁用) ：重製 PPU 狀態 ( LY = 0 , STAT = 0 )
        
    */

    public void Update(int cycles) {
        _countCycles += cycles;

        if (((_mmu.LCDC >> 7) & 1) == 1)
        {
            /* 進行 PPU 更新*/
            
            // 判斷 Mode ( STAT 低二位 )
            u8 mode = (u8) (_mmu.STAT & 0b00000011);
            // HBlank
            if (mode == 0b00)
            {
                if (_countCycles >= 204)
                {
                    _mmu.LY = (u8) (_mmu.LY + 1);
                    _countCycles -= 204;

                    if (_mmu.LY == Global.SCREEN_HEIGHT)
                    {
                        // Change State
                        _mmu.STAT = (u8) (_mmu.STAT & 0b11111100);
                        _mmu.STAT = (u8) (_mmu.STAT | 0b00000001);

                        // 中斷
                        if ((((_mmu.STAT >> 4) & 1 )== 1)) _mmu.IO[0x0F] = (u8) (_mmu.IO[0x0F] | ((u8) (1 << 1)));
                        _mmu.IO[0x0F] = (u8) (_mmu.IO[0x0F] | ((u8) (1 << 0)));

                        // 繪製畫面
                        Render();
                    }
                    else
                    {
                        // Change State
                        _mmu.STAT = (u8) (_mmu.STAT & 0b11111100);
                        _mmu.STAT = (u8) (_mmu.STAT | 0b00000010);

                        // 中斷
                        if ((((_mmu.STAT >> 5) & 1 )== 1)) _mmu.IO[0x0F] = (u8) (_mmu.IO[0x0F] | ((u8) (1 << 1)));
                    }
                }
            }
            // VBlank ( cycle = 456 )
            else if (mode == 0b01)
            {
                if (_countCycles >= 456)
                {
                    _mmu.LY = (u8) (_mmu.LY + 1);
                    _countCycles -= 456;

                    if (_mmu.LY > 153)
                    {
                        _mmu.STAT = (u8) (_mmu.STAT & 0b11111100);
                        _mmu.STAT = (u8) (_mmu.STAT | 0b00000010);
                        // 中斷
                        if (((_mmu.STAT >> 5) & 1) == 1) _mmu.IO[0x0F] = (u8) (_mmu.IO[0x0F] | ((u8) (1 << 1)));
                        // 重置掃描線
                        _mmu.LY = 0;
                    }
                }
            }
            // OAM 搜尋 ( cycle = 80 )
            else if (mode == 0b10)
            {
                if (_countCycles >= 80)
                {
                    // 進到 VRAM --> 修改 STAT 狀態
                    _mmu.STAT = (u8) (_mmu.STAT & 0b11111100);
                    _mmu.STAT = (u8) (_mmu.STAT | 0b00000011);
                    _countCycles -= 80;
                }
            }
            // VRAM 傳輸 ( cycle -> 172)
            else if (mode == 0b11)
            {
                if (_countCycles >= 172)
                {
                    // Change State
                    _mmu.STAT = (u8) (_mmu.STAT & 0b11111100);
                    _mmu.STAT = (u8) (_mmu.STAT | 0b00000000);
                    // 中斷
                    if ((((_mmu.STAT >> 3) & 1 )== 1)) _mmu.IO[0x0F] = (u8) (_mmu.IO[0x0F] | ((u8) (1 << 1)));

                    // 繪製掃描線
                    if (((_mmu.LCDC >> 0) & 1) == 1) BGToBuffer();
                    if (((_mmu.LCDC >> 5) & 1) == 1 && _mmu.WY <= _mmu.LY) WindowToBuffer();
                    if (((_mmu.LCDC >> 1) & 1) == 1) SpritesToBuffer();

                    _countCycles -= 172;
                }
            }
        
            if (_mmu.LY == _mmu.LYC)
            {
                _mmu.STAT = (u8) (_mmu.STAT | ((u8) (1 << 2)));
                // 中斷
                if (((_mmu.STAT >> 6) & 1) == 1) _mmu.IO[0x0F] = (u8) (_mmu.IO[0x0F] | ((u8) (1 << 1)));
            }
            else
            {
                _mmu.STAT = (u8) (_mmu.STAT & (~(1 << 2)));
            }
        }
        else
        {
            _countCycles = 0;
            _mmu.LY = 0;
            _mmu.STAT = (u8) (_mmu.STAT & 0b11111100);
        }
    }


    private void BGToBuffer() 
    {
        u16 tileMapBase = (u16) ((((_mmu.LCDC >> 3) & 1) == 1) ? 0x9C00 : 0x9800);
        u16 tileDataBase = (u16) ((((_mmu.LCDC >> 4) & 1) == 1) ? 0x8000 : 0x8800);

        for (int x = 0 ; x < Global.SCREEN_WIDTH ; x ++)
        {
            u8 pX = (u8) ((x + _mmu.SCX) & 0b11111111);
            u8 pY = (u8) ((_mmu.LY + _mmu.SCY) & 0b11111111);

            // 當前位址
            // 使用公式計算當前像素對應的 Tile 在 Tile Map 中的地址
            u16 tileAddress = (u16) (tileMapBase + (pY / 8) * 32 + pX / 8);
            // 從 Tile Map 中讀取 Tile ID，確定當前 Tile 的數據在 Tile Data 中的位置
            sbyte tileID = (sbyte) (_mmu.VRAM[tileAddress & 0x1FFF]);
            u16 tileDataAddress = (u16) (tileDataBase + ((((_mmu.LCDC >> 4) & 1) == 1) ? tileID : tileID + 128) * 16);

            // 當前像素在 Tile 中的垂直偏移量
            u8 tileLine = (u8) ((pY % 8) * 2);
            u8 low = _mmu.VRAM[(tileDataAddress + tileLine) & 0x1FFF];
            u8 high = _mmu.VRAM[(tileDataAddress + tileLine + 1) & 0x1FFF];

            // 計算當前像素在行內的水平偏移量
            int colorBias = 7 - (pX % 8);
            int index = (((high >> colorBias) & 1) << 1) | ((low >> colorBias) & 1);
            _frameBuffer[x, _mmu.LY] = _color[(_mmu.BGP >> (index * 2)) & 0x3];
        }
    }

    private void WindowToBuffer()
    {
        u8 WY = _mmu.WY;
        u8 WX = (u8) (_mmu.WX - 7);

        if (_mmu.LY >= WY)
        {
            u16 tileMapBase = (u16) ((((_mmu.LCDC >> 6) & 1) == 1) ? 0x9C00 : 0x9800);
            u16 tileDataBase = (u16) ((((_mmu.LCDC >> 4) & 1) == 1) ? 0x8000 : 0x8800);

            u8 winY = (u8) (_mmu.LY - WY);

            for (int x = 0 ; x < Global.SCREEN_WIDTH ; x ++) 
            {
                if (x >= WX) 
                {
                    u8 winX = (u8) (x - WX);

                    u16 tileAddress = (u16) (tileMapBase + (winY / 8) * 32 + (winX / 8));
                    int tileID = (int) _mmu.VRAM[tileAddress & 0x1FFF];
                    u16 tileDataAddress = (u16) (tileDataBase + ((((_mmu.LCDC >> 4) & 1) == 1) ? tileID : tileID + 128) * 16);

                    byte tileLine = (u8) ((winY % 8) * 2);
                    u8 low = _mmu.VRAM[(tileDataAddress + tileLine) & 0x1FFF];
                    u8 high = _mmu.VRAM[(tileDataAddress + tileLine + 1) & 0x1FFF];

                    int colorBias = 7 - (winX % 8);
                    int index = ((high >> colorBias) & 1) << 1 | ((low >> colorBias) & 1);
                    _frameBuffer[x, _mmu.LY] = _color[(_mmu.BGP >> (index * 2)) & 0b00000011];
                }
            }
        }
    }

    private void SpritesToBuffer() 
    {
        // byte LY = _mmu.LY;
        // byte LCDC = _mmu.LCDC;

        for (int i = 0x9C; i >= 0; i -= 4) {
            int y = _mmu.ReadOAM(i) - 16;
            int x = _mmu.ReadOAM(i + 1) - 8;
            byte tile = _mmu.ReadOAM(i + 2);
            byte attr = _mmu.ReadOAM(i + 3);

            if ((_mmu.LY >= y) && (_mmu.LY < y + (IsBit(2, _mmu.LCDC) ? 16 : 8))) {
                byte palette = IsBit(4, attr) ? _mmu.OBP1 : _mmu.OBP0;
                bool aboveBG = !IsBit(7, attr);

                int tileRow = IsBit(6, attr) ? (IsBit(2, _mmu.LCDC) ? 16 : 8) - 1 - (_mmu.LY - y) : (_mmu.LY - y);

                ushort tileAddress = (ushort)(0x8000 + tile * 16 + tileRow * 2);
                byte lo = _mmu.ReadVRAM(tileAddress);
                byte hi = _mmu.ReadVRAM((ushort)(tileAddress + 1));

                for (int p = 0; p < 8; p++) {
                    int colorBit = IsBit(5, attr) ? p : 7 - p;
                    int colorId = GetColorIdBits(colorBit, lo, hi);

                    if (colorId != 0) {
                        int paletteIndex = (palette >> (colorId * 2)) & 0x3;
                        int drawX = x + p;

                        // 添加邊界檢查
                        if (drawX >= 0 && drawX < Global.SCREEN_WIDTH && _mmu.LY >= 0 && _mmu.LY < Global.SCREEN_HEIGHT) {
                            if (aboveBG || _frameBuffer[drawX, _mmu.LY] == _color[0]) {
                                _frameBuffer[drawX, _mmu.LY] = _color[paletteIndex];
                            }
                        }
                    }
                }
            }
        }
    }


    private int GetColorIdBits(int colorBit, u8 l, u8 h) {
        int hi = (h >> colorBit) & 0x1;
        int lo = (l >> colorBit) & 0x1;
        return (hi << 1 | lo);
    }

    private async void Render() {
        Thread.Sleep((int) (17 * (1 / Global.GAME_SPEED)));
        Application.Invoke((sender, args) => _drawingArea.QueueDraw());
    }

    private void OnDrawn(object sender, DrawnArgs args) {
        using (Context cr = args.Cr) {
            cr.Antialias = Antialias.None;
            double scaleX = (double)_drawingArea.AllocatedWidth / Global.SCREEN_WIDTH;
            double scaleY = (double)_drawingArea.AllocatedHeight / Global.SCREEN_HEIGHT;

            cr.Scale(scaleX, scaleY);

            for (int y = 0; y < Global.SCREEN_HEIGHT; y++) {
                for (int x = 0; x < Global.SCREEN_WIDTH; x++) {
                    int colorValue = _frameBuffer[x, y];
                    double r = ((colorValue >> 16) & 0xFF) / 255.0;
                    double g = ((colorValue >> 8) & 0xFF) / 255.0;
                    double b = (colorValue & 0xFF) / 255.0;

                    cr.SetSourceRGB(r, g, b);
                    cr.Rectangle(x, y, 1, 1);
                    cr.Fill();
                }
            }
        }
    }

    private bool IsLCDEnabled(byte LCDC) => IsBit(7, LCDC);

    private ushort GetBGTileMapAddress(byte LCDC) => IsBit(3, LCDC) ? (ushort)0x9C00 : (ushort)0x9800;

    private bool IsBit(int n, int v) => ((v >> n) & 1) == 1;

    private u8 BitSet(u8 n, u8 v) => v |= (byte)(1 << n);

    private u8 BitClear(int n, u8 v) => v &= (byte)~(1 << n);
}
