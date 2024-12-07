/*
GB Manual Page35, 36
1. FF00 (P1)
    Name - P1
    Contents - Register for reading joy pad info
    and determining system type. (R/W)
    Bit 7 - Not used
    Bit 6 - Not used
    Bit 5 - P15 out port
    Bit 4 - P14 out port
    Bit 3 - P13 in port
    Bit 2 - P12 in port
    Bit 1 - P11 in port
    Bit 0 - P10 in port
This is the matrix layout for register $FF00:
    P14 P15
    | |
    P10-------O-Right----O-A
    | |
    P11-------O-Left-----O-B
    | |
    P12-------O-Up-------O-Select
    | |
    P13-------O-Down-----O-Start
    | |
*/
using Gdk;


public class Keyboard
{
    private const int JOYPAD_INTERRUPT = 4;
    private const u8 PAD_MASK = 0x10;
    private const u8 BUTTON_MASK = 0x20;
    private u8 _joyPad = 0xF;
    private u8 buttons = 0xF;

    internal void HandleKeyDown(EventKey e) {
        byte b = GetKeyBit(e);
        if ((b & PAD_MASK) == PAD_MASK) {
            _joyPad = (u8)(_joyPad & ~(b & 0xF));
        } else if ((b & BUTTON_MASK) == BUTTON_MASK) {
            buttons = (byte)(buttons & ~(b & 0xF));
        }
    }

    internal void HandleKeyUp(EventKey e) {
        byte b = GetKeyBit(e);
        if ((b & PAD_MASK) == PAD_MASK) {
            _joyPad = (u8)(_joyPad | (b & 0xF));
        } else if ((b & BUTTON_MASK) == BUTTON_MASK) {
            buttons = (u8)(buttons | (b & 0xF));
        }
    }

    public void Update(ref MMU mmu) {
        u8 JoyPad = mmu.JOYPAD;
        if (!IsBit(4, JoyPad)) {
            mmu.JOYPAD = ((u8)((JoyPad & 0xF0) | _joyPad));
            if (_joyPad != 0xF) 
            {
                mmu.RequestInterrupt(4);
            }
        }
        if (!IsBit(5, JoyPad)) {
            mmu.JOYPAD = ((u8)((JoyPad & 0xF0) | buttons));
            if (buttons != 0xF) 
            {
                mmu.RequestInterrupt(4);
            }
        }
        if ((JoyPad & 0b00110000) == 0b00110000) 
        {
            mmu.JOYPAD = (0xFF);
        }
    }

    private byte GetKeyBit(EventKey e) {
        if (e.Key == Gdk.Key.d || e.Key == Gdk.Key.Right)
        {
            return 0x11;
        }
        else if (e.Key == Gdk.Key.a || e.Key == Gdk.Key.Left)
        {
            return 0x12;
        }
        else if (e.Key == Gdk.Key.w || e.Key == Gdk.Key.Up)
        {
            return 0x14;
        }
        else if (e.Key == Gdk.Key.s || e.Key == Gdk.Key.Down)
        {
            return 0x18;
        }
        else if (e.Key == Gdk.Key.j || e.Key == Gdk.Key.z)
        {
            return 0x21;
        }
        else if (e.Key == Gdk.Key.k || e.Key == Gdk.Key.x)
        {
            return 0x22;
        }
        else if (e.Key == Gdk.Key.space || e.Key == Gdk.Key.c)
        {
            return 0x24;
        }
        else if (e.Key == Gdk.Key.Return || e.Key == Gdk.Key.v)
        {
            return 0x28;
        }
        else
        {
            return 0;
        }
    }

    private bool IsBit(int bit, byte value) {
        return (value & (1 << bit)) != 0;
    }
}