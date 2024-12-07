using Gtk;


public class Emulator
{
    private MMU _mmu;
    private CPU _cpu;
    private PPU _ppu;
    private Timer _timer;
    private Cartridge _cartridge;
    private Keyboard _keyboard;
    private bool _running = false;

    public Emulator(string filePath, DrawingArea drawingArea, Keyboard keyboard)
    {
        _cartridge = new Cartridge(filePath);
        IMBC mbc = _cartridge.GetMBC();
        _mmu = new MMU(ref mbc);
        _cpu = new CPU(ref _mmu);
        _ppu = new PPU(drawingArea);
        _timer = new Timer();
        _keyboard = keyboard; // 外部傳入的 Keyboard

        _running = true;
        Task t = Task.Run(() => Run());
    }

    public async Task Run()
    {
        int cycles = 0;
        int returnCycles = 0;
        while (_running)
        {
            while (cycles < 70224) // 一幀需要的時鐘週期數
            {
                returnCycles = _cpu.Step();
                cycles += returnCycles;

                // 更新 Timer
                _timer.UpdateDIV(ref _mmu, returnCycles);
                _timer.UpdateTIMA(ref _mmu, returnCycles);

                // 更新 PPU
                _ppu.Update(ref _mmu, returnCycles);

                // 更新鍵盤輸入
                _keyboard.Update(ref _mmu);

                // 中斷處理
                u8 IE = _mmu.GetIE();
                u8 IF = _mmu.IFRegister;
                for (int i = 0; i < 5; i++)
                {
                    if ((((IE & IF) >> i) & 0x1) == 1)
                    {
                        _cpu.ExecuteInterrupt(i);
                    }
                }
                _cpu.UpdateIME();
            }

            cycles -= 70224;
        }
    }
}
