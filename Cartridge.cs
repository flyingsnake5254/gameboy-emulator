public class Cartridge
{
    private u8[] _rom;
    private IMBC _IMBC;

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
            _IMBC = new MBC1(ref _rom);
        }
    }

    public IMBC GetMBC()
    {
        return _IMBC;
    }
}