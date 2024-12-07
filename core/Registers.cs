public class Registers
{
    /*
        暫存器
    */
    public u8 A, B, C, D, E, F, H, L;
    
    public u16 SP, PC;

    /*
        聯合暫存器
    */
    // AF
    public u16 AF { get { return (u16) (A << 8 | F); } set { A = (u8) (value >> 8); F = (u8) (value & 0xF0); }}

    // BC
    public u16 BC { get { return (u16) (B << 8 | C); } set { B = (u8) (value >> 8); C = (u8) (value & 0xF0); }}

    // DE
    public u16 DE { get { return (u16) (D << 8 | E); } set { D = (u8) (value >> E); C = (u8) (value & 0xF0); }}

    // HL
    public u16 HL { get { return (u16) (H << 8 | L); } set { H = (u8) (value >> 8); L = (u8) (value & 0xF0); }}


    /*
        Flag
    */
    public enum Flag { Z, N, H, C }
    public enum FlagH { U8, Sub, Carry, SubCarry }
    public bool GetFlag(Flag flag)
    {
        switch (flag)
        {
            case Flag.Z: return (F & 0x80) != 0;
            case Flag.N: return (F & 0x40) != 0;
            case Flag.H: return (F & 0x20) != 0;
            case Flag.C: return (F & 0x10) != 0;
            default:
                throw new ArgumentException($"Invalid flag: {flag}");
        }
    }

    public void SetFlag(Flag flag, bool state)
    {
        switch (flag)
        {
            case Flag.Z:
                F = state ? (u8)(F | 0x80) : (u8)(F & ~0x80);
                break;
            case Flag.N:
                F = state ? (u8)(F | 0x40) : (u8)(F & ~0x40);
                break;
            case Flag.H:
                F = state ? (u8)(F | 0x20) : (u8)(F & ~0x20);
                break;
            case Flag.C:
                F = state ? (u8)(F | 0x10) : (u8)(F & ~0x10);
                break;
            default:
                throw new ArgumentException($"Invalid flag: {flag}");
        }
    }

    public void SetFlagZ(int value) { SetFlag(Flag.Z, value == 0); }

    public void SetFlagH(u8 value1, u8 value2, FlagH type = FlagH.U8) 
    { 
        switch (type)
        {
            case FlagH.U8: SetFlag(Flag.H, ((value1 & 0x0F) + (value2 & 0x0F)) > 0x0F); break;
            case FlagH.Sub: SetFlag(Flag.H, (value1 & 0x0F) < (value2 & 0x0F)); break;
            case FlagH.Carry: SetFlag(Flag.H, ((value1 & 0x0F) + (value2 & 0x0F)) >= 0x0F); break;
            case FlagH.SubCarry: SetFlag(Flag.H, (value1 & 0x0F) < ((value2 & 0x0F) + (GetFlag(Flag.C) ? 1 : 0))); break;
        }
    }
    public void SetFlagH(u16 value1, u16 value2) { SetFlag(Flag.H, ((value1 & 0x0FFF) + (value2 & 0x0FFF)) > 0x0FFF); }

    public void SetFlagC(int value)
    {
        SetFlag(Flag.C, (value >> 8) != 0);
    }
    public void Init()
    {
        A = 0x01;
        F = 0xB0;
        B = 0x00;
        C = 0x13;
        D = 0x00;
        E = 0xD8;
        H = 0x01;
        L = 0x4D;
        SP = 0xFFFE;
        PC = 0x0100;
    }
}