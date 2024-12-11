public class CPU
{
    // 暫存器
    private Registers _regs;

    // 指令集
    private Instructions _insc;

    // Memory
    private MMU _mmu;


    public CPU(ref MMU mmu)
    {
        this._mmu = mmu;
        _regs = new Registers();
        _regs.Init();
        _insc = new Instructions(ref _mmu, ref _regs);
    }

    public int Step()
    {
        return _insc.Step();
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