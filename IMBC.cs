
public interface IMBC
{
    u8 Read(u16 address);
    void Write(u16 address, u8 value);
    u8 ReadERAM(u16 address);
    void WriteERAM(u16 address, u8 value);
}