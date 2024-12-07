using Gtk;
using Cairo;
using System.Runtime.CompilerServices;


public class PPU {

    private int[] _color = new int[] { 0x00FFFFFF, 0x00808080, 0x00404040, 0 }; // 調色板顏色
    private DrawingArea _drawingArea;
    private int[,] _frameBuffer = new int[Global.SCREEN_WIDTH, Global.SCREEN_HEIGHT];
    private int _scanlineCounter;

    public PPU(DrawingArea drawingArea) {
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

    public void Update(ref MMU mmu, int cycles) {
        _scanlineCounter += cycles;
        byte currentMode = (byte)(mmu.GetSTAT() & 0x3); // LCD 模式

        if (IsLCDEnabled(mmu.GetLCDC())) {
            switch (currentMode) {
                case 2: // OAM 掃描模式
                    if (_scanlineCounter >= 80) {
                        ChangeSTATMode(3, mmu);
                        _scanlineCounter -= 80;
                    }
                    break;
                case 3: // VRAM 模式
                    if (_scanlineCounter >= 172) {
                        ChangeSTATMode(0, mmu);
                        DrawScanLine(mmu);
                        _scanlineCounter -= 172;
                    }
                    break;
                case 0: // HBLANK 模式
                    if (_scanlineCounter >= 204) {
                        mmu.SetLY((byte)(mmu.GetLY() + 1));
                        _scanlineCounter -= 204;

                        if (mmu.GetLY() == Global.SCREEN_HEIGHT) {
                            ChangeSTATMode(1, mmu);
                            mmu.RequestInterrupt(0); // 發送 VBLANK 中斷
                            RenderFrame(); // 完成一幀後觸發繪製
                        } else {
                            ChangeSTATMode(2, mmu);
                        }
                    }
                    break;
                case 1: // VBLANK 模式
                    if (_scanlineCounter >= 456) {
                        mmu.SetLY((byte)(mmu.GetLY() + 1));
                        _scanlineCounter -= 456;

                        if (mmu.GetLY() > 153) {
                            ChangeSTATMode(2, mmu);
                            mmu.SetLY(0); // 重置掃描線
                        }
                    }
                    break;
            }

            // 設置 coincidence flag
            if (mmu.GetLY() == mmu.GetLYC()) {
                mmu.SetSTAT(BitSet(2, mmu.GetSTAT()));
                if (IsBit(6, mmu.GetSTAT())) mmu.RequestInterrupt(1);
            } else {
                mmu.SetSTAT(BitClear(2, mmu.GetSTAT()));
            }
        } 
        else {
            _scanlineCounter = 0;
            mmu.SetLY(0);
            mmu.SetSTAT((byte)(mmu.GetSTAT() & ~0x3));
        }
    }

    private void ChangeSTATMode(int mode, MMU mmu) {
        byte STAT = (byte)(mmu.GetSTAT() & ~0x3);
        mmu.SetSTAT((byte)(STAT | mode));

        if (mode == 2 && IsBit(5, STAT)) mmu.RequestInterrupt(1); // OAM 中斷
        else if (mode == 0 && IsBit(3, STAT)) mmu.RequestInterrupt(1); // HBLANK 中斷
        else if (mode == 1 && IsBit(4, STAT)) mmu.RequestInterrupt(1); // VBLANK 中斷
    }

    private void DrawScanLine(MMU mmu) {
        byte LCDC = mmu.GetLCDC();
        if (IsBit(0, LCDC)) RenderBG(mmu);
        if (IsBit(5, LCDC) && mmu.WY <= mmu.GetLY()) RenderWindow(mmu);
        if (IsBit(1, LCDC)) RenderSprites(mmu);
    }

    private void RenderBG(MMU mmu) {
        byte LY = mmu.GetLY();
        byte SCY = mmu.GetSCY();
        byte SCX = mmu.GetSCX();
        ushort tileMapBase = GetBGTileMapAddress(mmu.GetLCDC());
        ushort tileDataBase = IsBit(4, mmu.GetLCDC()) ? (ushort)0x8000 : (ushort)0x8800;

        for (int x = 0; x < Global.SCREEN_WIDTH; x++) {
            byte pixelX = (byte)((x + SCX) & 0xFF);
            byte pixelY = (byte)((LY + SCY) & 0xFF);

            ushort tileAddress = (ushort)(tileMapBase + (pixelY / 8) * 32 + (pixelX / 8));
            sbyte tileId = (sbyte)mmu.ReadVRAM(tileAddress);
            ushort tileDataAddress = (ushort)(tileDataBase + (IsBit(4, mmu.GetLCDC()) ? tileId : tileId + 128) * 16);

            byte tileLine = (byte)((pixelY % 8) * 2);
            byte lo = mmu.ReadVRAM((ushort)(tileDataAddress + tileLine));
            byte hi = mmu.ReadVRAM((ushort)(tileDataAddress + tileLine + 1));

            int colorBit = 7 - (pixelX % 8);
            int paletteIndex = ((hi >> colorBit) & 1) << 1 | ((lo >> colorBit) & 1);
            _frameBuffer[x, LY] = _color[(mmu.BGP >> (paletteIndex * 2)) & 0x3];
        }
    }

    private void RenderWindow(MMU mmu) {
        byte WY = mmu.WY;
        byte WX = (byte)(mmu.WX - 7);
        byte LY = mmu.GetLY();

        if (LY >= WY) {
            ushort tileMapBase = IsBit(6, mmu.GetLCDC()) ? (ushort)0x9C00 : (ushort)0x9800;
            ushort tileDataBase = IsBit(4, mmu.GetLCDC()) ? (ushort)0x8000 : (ushort)0x8800;

            byte windowY = (byte)(LY - WY);

            for (int x = 0; x < Global.SCREEN_WIDTH; x++) {
                if (x >= WX) {
                    byte windowX = (byte)(x - WX);

                    ushort tileAddress = (ushort)(tileMapBase + (windowY / 8) * 32 + (windowX / 8));
                    sbyte tileId = (sbyte)mmu.ReadVRAM(tileAddress);
                    ushort tileDataAddress = (ushort)(tileDataBase + (IsBit(4, mmu.GetLCDC()) ? tileId : tileId + 128) * 16);

                    byte tileLine = (byte)((windowY % 8) * 2);
                    byte lo = mmu.ReadVRAM((ushort)(tileDataAddress + tileLine));
                    byte hi = mmu.ReadVRAM((ushort)(tileDataAddress + tileLine + 1));

                    int colorBit = 7 - (windowX % 8);
                    int paletteIndex = ((hi >> colorBit) & 1) << 1 | ((lo >> colorBit) & 1);
                    _frameBuffer[x, LY] = _color[(mmu.BGP >> (paletteIndex * 2)) & 0x3];
                }
            }
        }
    }

    private void RenderSprites(MMU mmu) {
        byte LY = mmu.GetLY();
        byte LCDC = mmu.GetLCDC();

        for (int i = 0x9C; i >= 0; i -= 4) {
            int y = mmu.ReadOAM(i) - 16;
            int x = mmu.ReadOAM(i + 1) - 8;
            byte tile = mmu.ReadOAM(i + 2);
            byte attr = mmu.ReadOAM(i + 3);

            if ((LY >= y) && (LY < y + (IsBit(2, LCDC) ? 16 : 8))) {
                byte palette = IsBit(4, attr) ? mmu.OBP1 : mmu.OBP0;
                bool aboveBG = !IsBit(7, attr);

                int tileRow = IsBit(6, attr) ? (IsBit(2, LCDC) ? 16 : 8) - 1 - (LY - y) : (LY - y);

                ushort tileAddress = (ushort)(0x8000 + tile * 16 + tileRow * 2);
                byte lo = mmu.ReadVRAM(tileAddress);
                byte hi = mmu.ReadVRAM((ushort)(tileAddress + 1));

                for (int p = 0; p < 8; p++) {
                    int colorBit = IsBit(5, attr) ? p : 7 - p;
                    int colorId = GetColorIdBits(colorBit, lo, hi);

                    if (colorId != 0) {
                        int paletteIndex = (palette >> (colorId * 2)) & 0x3;
                        int drawX = x + p;

                        // 添加邊界檢查
                        if (drawX >= 0 && drawX < Global.SCREEN_WIDTH && LY >= 0 && LY < Global.SCREEN_HEIGHT) {
                            if (aboveBG || _frameBuffer[drawX, LY] == _color[0]) {
                                _frameBuffer[drawX, LY] = _color[paletteIndex];
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

    private async void RenderFrame() {
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
