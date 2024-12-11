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
        _insc = new Instructions(ref _mmu);
    }

    public int Step()
    {
        int temp = _insc.Step();
        return temp;
    }

    public void Interrupt(int value)
    {
        _insc.ExecuteInterrupt(value);
    }

    public void UpdateIME()
    {
        _insc.UpdateIME();
    }

}