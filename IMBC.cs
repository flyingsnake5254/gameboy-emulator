using u8 = System.Byte;
using u16 = System.UInt16;
public interface IMBC
{
    u8 ReadROM(u16 address);
    void WriteROM(u16 address, u8 value);
    u8 ReadERAM(u16 address);
    void WriteERAM(u16 address, u8 value);
}