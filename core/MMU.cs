
public class MMU
{
    public u8[] IO = new u8[0xFF7F - 0xFF00 + 1];
    public u8[] VRAM = new u8[0x9FFF - 0x8000 + 1];
    public u8[] WRAM = new u8[0xDFFF - 0xC000 + 1];
    private u8[] WRAM0 = new u8[0x1000];
    private u8[] WRAM1 = new u8[0x1000];
    public u8[] OAM = new u8[0xFE9F - 0xFE00 + 1];
    private u8[] HRAM = new u8[0x80];

    public u8 IFRegister { get { return IO[0x0F]; } set { IO[0x0F] = value; }}
    public u8 LY { get { return IO[0x44]; } set { IO[0x44] = value; }}
    public u8 LCDC { get { return IO[0x40]; } set { IO[0x40] = value;}}
    public u8 STAT { get { return IO[0x41]; } set { IO[0x41] = value; }}
    public u8 SCX { get { return IO[0x43]; } set { IO[0x43] = value; }}
    public u8 SCY { get { return IO[0x42]; } set { IO[0x42] = value; }}
    public u8 BGP { get { return IO[0x47]; } set { IO[0x47] = value; }}
    public u8 WY { get { return IO[0x4A]; } set { IO[0x4A] = value; }}
    public u8 WX { get { return IO[0x4B]; } set { IO[0x4B] = value; }}
    public u8 OBP1 { get { return IO[0x49]; } set { IO[0x49] = value; }}
    public u8 OBP0 { get { return IO[0x48]; } set { IO[0x48] = value; }}
    public u8 LYC { get { return IO[0x45]; } set { IO[0x45] = value; }}
    public u8 DIV { get { return IO[0x04]; } set { IO[0x04] = value; }}
    public u8 TAC { get { return IO[0x07]; } set { IO[0x07] = value; }}
    public u8 TIMA { get { return IO[0x05]; } set { IO[0x05] = value; }}
    public u8 JOYPAD { get { return IO[0x00]; } set { IO[0x00] = value; }}

    private IMBC _mbc;
    

    public MMU(ref IMBC mbc)
    {
        this._mbc = mbc;
        IOInit();
    }

    public u8 ReadROM(u16 address)
    {
        if (address < 0x8000)
        {
            return _mbc.ReadROM(address);
        }
        else if (address < 0xA000)
        {
            return VRAM[address & 0x1FFF];
        }
        else if (address < 0xC000)
        {
            return _mbc.ReadERAM(address);
        }
        else if (address < 0xD000)
        {
            return WRAM0[address & 0xFFF];
        }
        else if (address < 0xE000)
        {
            return WRAM1[address & 0xFFF];
        }
        else if (address < 0xF000)
        {
            return WRAM0[address & 0xFFF];
        }
        else if (address < 0xFEA0)
        {
            return OAM[address & (0xFE9F - 0xFE00)];
        }
        else if (address < 0xFF00)
        {
            return 0;
        }
        else if (address < 0xFF80)
        {
            return IO[address & 0x7F];
        }
        else if (address <= 0xFFFF)
        {
            return HRAM[address & 0x7F];
        }
        else
        {
            return 0xFF;
        }

    }

    public void WriteROM(u16 address, u8 value)
    {
        if (address < 0x8000) _mbc.WriteROM(address, value);
        else if (address < 0xA000) VRAM[address & 0x1FFF] = value;
        else if (address < 0xC000) _mbc.WriteERAM(address, value);
        else if (address < 0xD000) WRAM0[address & 0xFFF] = value;
        else if (address < 0xE000) WRAM1[address & 0xFFF] = value;
        else if (address < 0xF000) WRAM0[address & 0xFFF] = value;
        else if (address < 0xFE00) WRAM1[address & 0xFFF] = value;
        else if (address < 0xFEA0) OAM[address & 0x9F] = value;
        else if (address < 0xFF00) ; // Not Usable
        else if (address < 0xFF80) 
        {
            if (address == 0xFF0F) IO[address & 0x7F] = (u8) (value | 0xE0);
            else if (address == 0xFF04 || address == 0xFF44) IO[address & 0x7F] = 0;
            else if (address == 0xFF46)
            {
                u16 tempAddr = (u16) (value << 8);
                for (u8 i = 0 ; i < OAM.Length ; i ++)
                {
                    OAM[i] = ReadROM((u16) (tempAddr + i));
                }
            }
            else IO[address & 0x7F] = value;
        }
        else HRAM[address & 0x7F] = value;
    }
    public u16 ReadROM16(u16 address)
    {
        return (u16)(ReadROM((u16)(address + 1)) << 8 | ReadROM(address));
    }
    
    public void WriteROM16(u16 address, u16 value)
    {
        WriteROM((u16)(address + 1), (u8)(value >> 8));
        WriteROM(address, (u8) value);
    }

    public u8 ReadOAM(int address)
    {
        return OAM[address];
    }
    public void IOInit()
    {
        // IO[0x05] = 0x00; // TIMA
        // IO[0x06] = 0x00; // TMA
        // IO[0x07] = 0x00; // TAC
        IO[0x4D] = 0xFF;
        IO[0x10] = 0x80; // NR10
        IO[0x11] = 0xBF; // NR11
        IO[0x12] = 0xF3; // NR12
        IO[0x14] = 0xBF; // NR14
        IO[0x16] = 0x3F; // NR21
        // IO[0x17] = 0x00; // NR22
        IO[0x19] = 0xBF; // NR24
        IO[0x1A] = 0x7F; // NR30
        IO[0x1B] = 0xFF; // NR31
        IO[0x1C] = 0x9F; // NR32
        IO[0x1E] = 0xBF; // NR33
        IO[0x20] = 0xFF; // NR41
        // IO[0x21] = 0x00; // NR42
        // IO[0x22] = 0x00; // NR43
        IO[0x23] = 0xBF; // NR30
        IO[0x24] = 0x77; // NR50
        IO[0x25] = 0xF3; // NR51
        IO[0x26] = 0xF1; // NR52 (GB模式), 可選 0xF0 (SGB模式)
        IO[0x40] = 0x91; // LCDC
        // IO[0x42] = 0x00; // SCY
        // IO[0x43] = 0x00; // SCX
        // IO[0x45] = 0x00; // LYC
        IO[0x47] = 0xFC; // BGP
        IO[0x48] = 0xFF; // OBP0
        IO[0x49] = 0xFF; // OBP1
        // IO[0x4A] = 0x00; // WY
        // IO[0x4B] = 0x00; // WX
        // IO[0xFF] = 0x00; // IE (Interrupt Enable)
    }

    public void SetDIV(u8 value)
    {
        IO[0x04] = value;
    }

    public u8 GetDIV()
    {
        return IO[0x04];
    }

    public bool GetTACState()
    {
        return (IO[0x07] & 0b00000100) == 1;
    }

    public int GetTACFrequence()
    {
        /* 
        GB Manual Page 39
        7. FF07 (TAC)
            Name - TAC
            Contents - Timer Control (R/W)
            Bit 2 - Timer Stop
            0: Stop Timer
            1: Start Timer
            Bits 1+0 - Input Clock Select
            00: 4.096 KHz (~4.194 KHz SGB)
            01: 262.144 Khz (~268.4 KHz SGB)
            10: 65.536 KHz (~67.11 KHz SGB)
            11: 16.384 KHz (~16.78 KHz SGB)
        */

        switch (IO[0x07] & 0b00000011)
        {
            case 0:
                return 1024;
            case 1:
                return 16;
            case 2:
                return 64;
            case 3:
                return 256;
            default:
                return 0;
        }
    }

    public u8 GetTIMA()
    {
        return IO[0x05];
    }

    public void SetTIMA(u8 value)
    {
        IO[0x05] = value;
    }

    public void RequestInterrupt(u8 b) {
        IO[0x0F] |= (u8)(1 << b);
    }

    public u8 GetTMA()
    {
        return IO[0x06];
    }

    public u8 GetJoyPad()
    {
        return IO[0x00];
    }

    public void SetJoyPad(u8 value)
    {
        IO[0x00] = value;
    }

    public u8 GetSTAT()
    {
        return IO[0x41]; 
    }

    public void SetSTAT(u8 value)
    {
        IO[0x41] = value;
    }

    public u8 GetLCDC()
    {
        return IO[0x40]; 
    }

    public void SetLCDC(u8 value)
    {
        IO[0x40] = value;
    }

    public u8 GetLY()
    {
        return IO[0x44]; 
    }

    public void SetLY(u8 value)
    {
        IO[0x44] = value;
    }

    public u8 GetLYC()
    {
        return IO[0x45]; 
    }

    public void SetLYC(u8 value)
    {
        IO[0x45] = value;
    }

    public u8 GetSCX()
    {
        return IO[0x43]; 
    }

    public void SetSCX(u8 value)
    {
        IO[0x43] = value;
    }

    public u8 GetSCY()
    {
        return IO[0x42]; 
    }

    public void SetSCY(u8 value)
    {
        IO[0x42] = value;
    }

    public u8 GetBGP()
    {
        return IO[0x47]; 
    }

    public void SetBGP(u8 value)
    {
        IO[0x47] = value;
    }

    public u8 GetIE()
    {
        return HRAM[0x7F]; 
    }

    public void SetIE(u8 value)
    {
        HRAM[0x7F] = value;
    }

    public u8 GetIF()
    {
        return IO[0x0F]; 
    }


    public u8 ReadVRAM(u16 address)
    {
        return VRAM[address & 0x1FFF];
    }
}