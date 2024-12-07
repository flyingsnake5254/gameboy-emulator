public class Timer
{
    private int _div = 0;
    private int _tima = 0;
    private int[] _freq = {1024, 16, 64, 256};
    private MMU _mmu;

    public Timer(ref MMU mmu)
    {
        this._mmu = mmu;
    }

    public void UpdateDIV(int cycles)
    {
        _div += cycles;
        while (_div >= 256)
        {
            _mmu.DIV += 1;
            _div -= 256;
        }
    }

    public void UpdateTIMA(int cycles)
    {
        if ((_mmu.TAC & 0b00000100) == 1)
        {
            _tima += cycles;
            while (_tima >= _freq[_mmu.TAC & 0b00000011])
            {
                _mmu.TIMA += 1;
                _tima -= _freq[_mmu.TAC & 0b00000011];
            }

            if (_mmu.TIMA == 0xFF)
            {
                _mmu.IFRegister |= (u8) (1 << 2);
            }
        }
    }
}