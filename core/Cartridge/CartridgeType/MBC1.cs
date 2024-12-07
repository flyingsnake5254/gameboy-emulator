
/*
0x0000 - 0x3FFF : Bank0
0x4000 - 0x7FFF : Bank1-N
*/
public class MBC1 : ICartridgeType
{
    private u8[] _rom;
    private u8[] _eram = new u8[0x8000];
    private int _ROMBank = 1;
    private int _RAMBank;
    private int _bankMode; // 0:ROM Bank, 1:RAM Bank
    private bool _enableERAM;
    public MBC1(ref u8[] rom)
    {
        _rom = rom;
    }

    public u8 Read(u16 address)
    {
        if (address < 0x4000)
        {
            return _rom[address];
        }
        else if (address < 0x8000)
        {
            return _rom[(0x4000 * _ROMBank + (address & 0x3FFF))];
        }

        return 0;
    }

    public void Write(u16 address, u8 value)
    {
        if (address < 0x2000)
        {
            _enableERAM = value == 0x0A ? true : false;
        }
        else if (address < 0x4000)
        {
            _ROMBank = value & 0b00011111;
            if (_ROMBank == 0x00 || _ROMBank == 0x20 || _ROMBank == 0x40 || _ROMBank == 0x60)
            {
                _ROMBank += 1;
            }
        }
        else if (address < 0x6000)
        {
            // ROM Bank
            if (_bankMode == 0)
            {
                _ROMBank |= value & 0b00000011;
                if (_ROMBank == 0x00 || _ROMBank == 0x20 || _ROMBank == 0x40 || _ROMBank == 0x60)
                {
                    _ROMBank += 1;
                }
            }
            else
            {
                _RAMBank = value & 0b00000011;
            }
        }
        else if (address <= 0x8000)
        {
            _bankMode = value & 0b00000001;
        }
    }

    
    public u8 ReadERAM(u16 address)
    {
        if (_enableERAM)
        {
            return _eram[(0x2000 * _RAMBank + (address & 0x1FFF))];
        }
        return 0xFF;
    }

    public void WriteERAM(u16 address, u8 value)
    {
        if (_enableERAM)
        {
            _eram[0x2000 * _RAMBank + (address & 0x1FFF)] = value;
        }
    }
}