using Gdk;

public class Keyboard
{
    private const u8 PAD_MASK = 0x10;
    private const u8 BUTTON_MASK = 0x20;
    private u8 _joyPad = 0xF;
    private u8 buttons = 0xF;
    private MMU _mmu;
    
    public void Init(ref MMU mmu)
    {
        this._mmu = mmu;
    }
    internal void HandleKeyDown(EventKey e) 
    {
        u8 b = GetKeyBit(e);
        if ((b & PAD_MASK) == PAD_MASK) { _joyPad = (u8)(_joyPad & ~(b & 0xF)); } 
        else if ((b & BUTTON_MASK) == BUTTON_MASK) { buttons = (u8)(buttons & ~(b & 0xF)); }
    }

    internal void HandleKeyUp(EventKey e) 
    {
        u8 b = GetKeyBit(e);
        if ((b & PAD_MASK) == PAD_MASK) { _joyPad = (u8)(_joyPad | (b & 0xF)); } 
        else if ((b & BUTTON_MASK) == BUTTON_MASK) { buttons = (u8)(buttons | (b & 0xF)); }
    }

    public void Update() 
    {
        u8 JoyPad = _mmu.JOYPAD;
        if (!IsBit(4, JoyPad)) 
        {
            _mmu.JOYPAD = (u8)((JoyPad & 0xF0) | _joyPad);
            if (_joyPad != 0xF) { _mmu.IFRegister |= (u8) (1 << 4); }
        }
        if (!IsBit(5, JoyPad)) 
        {
            _mmu.JOYPAD = (u8)((JoyPad & 0xF0) | buttons);
            if (buttons != 0xF) { _mmu.IFRegister |= (u8) (1 << 4); }
        }
        if ((JoyPad & 0b00110000) == 0b00110000) { _mmu.JOYPAD = 0xFF; }
    }

    private u8 GetKeyBit(EventKey e) 
    {
        if (e.Key == Gdk.Key.d || e.Key == Gdk.Key.Right) { return 0x11; }
        else if (e.Key == Gdk.Key.a || e.Key == Gdk.Key.Left) { return 0x12; }
        else if (e.Key == Gdk.Key.w || e.Key == Gdk.Key.Up) { return 0x14; }
        else if (e.Key == Gdk.Key.s || e.Key == Gdk.Key.Down) { return 0x18; }
        else if (e.Key == Gdk.Key.j || e.Key == Gdk.Key.z) { return 0x21; }
        else if (e.Key == Gdk.Key.k || e.Key == Gdk.Key.x) { return 0x22; }
        else if (e.Key == Gdk.Key.space || e.Key == Gdk.Key.c) { return 0x24; }
        else if (e.Key == Gdk.Key.Return || e.Key == Gdk.Key.v) { return 0x28; }
        else { return 0; }
    }
    private bool IsBit(int bit, u8 value) {
        return (value & (1 << bit)) != 0;
    }
}