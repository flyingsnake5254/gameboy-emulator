public class CPU
{
    // 暫存器
    public Registers Regs;

    // 指令集
    private Instructions _insc;

    // Memory
    private MMU _mmu;

    private long tick = 0;


    public CPU(ref MMU mmu)
    {
        this._mmu = mmu;
        Regs = new Registers();
        Regs.Init();
        _insc = new Instructions(ref Regs, ref _mmu);
    }

    public int Step()
    {
        
        
        for (int i = 0x00 ; i <= 0xFF ; i ++)
        {
            Console.WriteLine($"{i, 0:X2} - {_insc.Execute((u8)i)}");
        }
        Console.Read();
        u8 opcode = _mmu.Read(Regs.PC ++);
        int temp = _insc.Execute(opcode);
        return temp;
    }

    public void Interrupt(int value)
    {
        _insc.Interrupt(value);
    }

    public void UpdateIME()
    {
        _insc.UpdateIME();
    }

}