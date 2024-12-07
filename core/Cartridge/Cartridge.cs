public class Cartridge
{
    private u8[] _rom;
    private ICartridgeType _ICartridgeType;

    private string _filePath;

    public Cartridge(string filePath)
    {
        this._filePath = filePath;
        LoadCart();
    }

    private void LoadCart()
    {
        _rom = File.ReadAllBytes(_filePath);
        
        // Cartridge Type
        if (_rom[0x147] == 0x01) // MBC1
        {
            _ICartridgeType = new MBC1(ref _rom);
        }
    }

    public ICartridgeType GetMBC()
    {
        return _ICartridgeType;
    }
}