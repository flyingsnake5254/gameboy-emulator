public interface ICartridgeType
{
    /*
        ROM
    */
    u8 ReadROM(u16 address);
    void WriteROM(u16 address, u8 value);

    /*
        External RAM
    */
    u8 ReadExternalRAM(u16 address);
    void WriteExternalRAM(u16 address, u8 value);
}