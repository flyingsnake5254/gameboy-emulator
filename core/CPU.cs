
public class CPU
{
    public Registers Regs;
    private Instructions _insc;
    private MMU _mmu;
    
    private ushort PC;
    private ushort SP;

    private byte A, B, C, D, E, F, H, L;

    private ushort AF { get { return (ushort)(A << 8 | F); } set { A = (byte)(value >> 8); F = (byte)(value & 0xF0); } }
    private ushort BC { get { return (ushort)(B << 8 | C); } set { B = (byte)(value >> 8); C = (byte)value; } }
    private ushort DE { get { return (ushort)(D << 8 | E); } set { D = (byte)(value >> 8); E = (byte)value; } }
    private ushort HL { get { return (ushort)(H << 8 | L); } set { H = (byte)(value >> 8); L = (byte)value; } }

    private bool FlagZ { get { return (F & 0x80) != 0; } set { F = value ? (byte)(F | 0x80) : (byte)(F & ~0x80); } }
    private bool FlagN { get { return (F & 0x40) != 0; } set { F = value ? (byte)(F | 0x40) : (byte)(F & ~0x40); } }
    private bool FlagH { get { return (F & 0x20) != 0; } set { F = value ? (byte)(F | 0x20) : (byte)(F & ~0x20); } }
    private bool FlagC { get { return (F & 0x10) != 0; } set { F = value ? (byte)(F | 0x10) : (byte)(F & ~0x10); } }

    private bool IME;
    private bool IMEEnabler;
    private bool HALTED;
    private bool HALT_BUG;
    private int cycles;
    private int[] outputs = new int[10000];
    private int count = 0;

    public CPU(ref MMU mmu) {
        this._mmu = mmu;
        Regs = new Registers();
        Regs.Init();
        _insc = new Instructions(ref Regs, ref _mmu);
        AF = Regs.AF;
        BC = Regs.BC;
        DE = Regs.DE;
        HL = Regs.HL;
        SP = Regs.SP;
        PC = Regs.PC;
    }
    public enum IncsType
    {
        R16_D16,
        MAddr_R8,
        R16,
        R8,
        MAddr,
        R8_D8,
        A16_R16,
        R16_R16,
        R8_MAddr,
        MAddr_D8,
        R8_R8,
        A16,
        D8,
        A8_R8,
        R8_A8,
        MAddr8_R8,
        R8_MAddr8,
        R16_S8,
        R16_R16S8,
        A16_R8,
        R8_A16,
        NO_CYCLE,
    }
    private long tick = 0;

    private int[] _incCycles = {
        //0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
        4, 12,  8,  8,  4,  4,  8,  4, 20,  8,  8,  8,  4,  4,  8,  4, //0
        4, 12,  8,  8,  4,  4,  8,  4,  0,  8,  8,  8,  4,  4,  8,  4, //1
        0, 12,  8,  8,  4,  4,  8,  4,  0,  8,  8,  8,  4,  4,  8,  4, //2
        0, 12,  8,  8, 12, 12, 12,  4,  0,  8,  8,  8,  4,  4,  8,  4, //3 

        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //4
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //5
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //6
        8,  8,  8,  8,  8,  8,  4,  8,  4,  4,  4,  4,  4,  4,  8,  4, //7

        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //8
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //9
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //A
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4, //B

        0,  12,  0,  0,  0, 16, 8, 16,  0, -4,  0,  0,  0,  0,  8, 16, //C
        0,  12,  0, 00,  0, 16, 8, 16,  0, -4,  0, 00,  0, 00,  8, 16, //D
        12, 12,  8, 00, 00, 16, 8, 16, 16,  4, 16, 00, 00, 00,  8, 16, //E
        12, 12,  8,  4, 00, 16, 8, 16, 12,  8, 16,  4, 00, 00,  8, 16  //F
    };

    public int Step() {

        byte opcode = _mmu.Read(PC++);
        // if (tick >= 8000)
        // {
        //     Console.WriteLine($"{tick ++, 0:D10} {opcode, 0:X2} AF:{AF, 0:X2} BC:{BC, 0:X2} DE:{DE, 0:X2} HL:{HL, 0:X2} PC:{PC, 0:X2} SP:{SP, 0:X2} {(FlagZ ? "Z" : "-")}{(FlagN ? "N" : "-")}{(FlagH ? "H" : "-")}{(FlagC ? "C" : "-")}");
        // }
        // else if (tick == 20000)
        // {
        //     Console.WriteLine("Stop");
        // }
        // Console.WriteLine($"{tick ++, 0:D10} {opcode, 0:X2} AF:{AF, 0:X2} BC:{BC, 0:X2} DE:{DE, 0:X2} HL:{HL, 0:X2} PC:{PC, 0:X2} SP:{SP, 0:X2} {(FlagZ ? "Z" : "-")}{(FlagN ? "N" : "-")}{(FlagH ? "H" : "-")}{(FlagC ? "C" : "-")}");
        // debug(opcode);
        // if (HALT_BUG) {
        //     PC--;
        //     HALT_BUG = false;
        // }
        cycles = 0;

        // File.AppendAllText("/home/sherloxk/Documents/output.txt", opcode + Environment.NewLine);
        // if (count < 10000)
        // {
        //     Console.WriteLine("save");
        //     outputs[count++] = opcode;
        // }
        // else
        // {
        //     string k = string.Join(",", outputs);
        //     count = 0;
        //     // File.AppendAllText("/home/sherloxk/Documents/output.txt", string.Join($"{Environment.NewLine}", outputs) + Environment.NewLine);

        //     Array.Clear(outputs);
        // }
        switch (opcode) {
            case 0x00:                              break; //NOP        1 4     ----
            case 0x01: LD(ref cycles, IncsType.R16_D16, "BC"); return cycles; break;//BC = _mmu.ReadROM16(PC); PC += 2;      ; break; //LD BC,D16  3 12    ----
            case 0x02: LD(ref cycles, IncsType.MAddr_R8, "BC", "A"); return cycles; break;//_mmu.Write(BC, A);                ;break; //LD (BC),A  1 8     ----
            case 0x03: BC += 1;                             break; //INC BC     1 8     ----
            case 0x04: B = INC(B);                          break; //INC B      1 4     Z0H-
            case 0x05: B = DEC(B);                          break; //DEC B      1 4     Z1H-
            case 0x06: LD(ref cycles, IncsType.R8_D8, "B"); return cycles; break;//B = _mmu.Read(PC); PC += 1;       ;break; //LD B,D8    2 8     ----

            case 0x07: //RLCA 1 4 000C
                F = 0;
                FlagC = ((A & 0x80) != 0);
                A = (byte)((A << 1) | (A >> 7));
                break;

            case 0x08: LD(ref cycles, IncsType.A16_R16); return cycles; break;//_mmu.WriteROM16(_mmu.ReadROM16(PC), SP); PC += 2; ;break; //LD (A16),SP 3 20   ----
            case 0x09: DAD(BC);                             break; //ADD HL,BC   1 8    -0HC
            case 0x0A: LD(ref cycles, IncsType.R8_MAddr, "A", "BC"); return cycles; break;//A = _mmu.Read(BC);  ;              break; //LD A,(BC)   1 8    ----
            case 0x0B: BC -= 1;                             break; //DEC BC      1 8    ----
            case 0x0C: C = INC(C);                          break; //INC C       1 8    Z0H-
            case 0x0D: C = DEC(C);                          break; //DEC C       1 8    Z1H-
            case 0x0E: LD(ref cycles, IncsType.R8_D8, "C"); return cycles; break;//C = _mmu.Read(PC); PC += 1;  ;     break; //LD C,D8     2 8    ----

            case 0x0F: //RRCA 1 4 000C
                F = 0;
                FlagC = ((A & 0x1) != 0);
                A = (byte)((A >> 1) | (A << 7));
                break;

            case 0x11: LD(ref cycles, IncsType.R16_D16, "DE"); return cycles; break;//DE = _mmu.ReadROM16(PC); PC += 2; ;     break; //LD DE,D16   3 12   ----
            case 0x12: LD(ref cycles, IncsType.MAddr_R8, "DE", "A"); return cycles; break;//_mmu.Write(DE, A);   ;             break; //LD (DE),A   1 8    ----
            case 0x13: DE += 1;                             break; //INC DE      1 8    ----
            case 0x14: D = INC(D);                          break; //INC D       1 8    Z0H-
            case 0x15: D = DEC(D);                          break; //DEC D       1 8    Z1H-
            case 0x16: LD(ref cycles, IncsType.R8_D8, "D"); return cycles; break;//D = _mmu.Read(PC); PC += 1;  ;     break; //LD D,D8     2 8    ----

            case 0x17://RLA 1 4 000C
                bool prevC = FlagC;
                F = 0;
                FlagC = ((A & 0x80) != 0);
                A = (byte)((A << 1) | (prevC ? 1 : 0));
                break;

            case 0x18: JR(true);                       break; //JR R8       2 12   ----
            case 0x19: DAD(DE);                             break; //ADD HL,DE   1 8    -0HC
            case 0x1A: LD(ref cycles, IncsType.R8_MAddr, "A", "DE"); return cycles; break;//A = _mmu.Read(DE);     ;           break; //LD A,(DE)   1 8    ----
            case 0x1B: DE -= 1;                             break; //INC DE      1 8    ----
            case 0x1C: E = INC(E);                          break; //INC E       1 8    Z0H-
            case 0x1D: E = DEC(E);                          break; //DEC E       1 8    Z1H-
            case 0x1E: LD(ref cycles, IncsType.R8_D8, "E"); return cycles; break;//E = _mmu.Read(PC); PC += 1;;       break; //LD E,D8     2 8    ----

            case 0x1F://RRA 1 4 000C
                bool preC = FlagC;
                F = 0;
                FlagC = ((A & 0x1) != 0);
                A = (byte)((A >> 1) | (preC ? 0x80 : 0));
                break;

            case 0x20: JR(!FlagZ);                     break; //JR NZ R8    2 12/8 ---- 
            case 0x21: LD(ref cycles, IncsType.R16_D16, "HL"); return cycles; break;//HL = _mmu.ReadROM16(PC); PC += 2;      break; //LD HL,D16   3 12   ----
            case 0x22: LD(ref cycles, IncsType.MAddr_R8, "HL+", "A"); return cycles; break;//_mmu.Write(HL++, A);              break; //LD (HL+),A  1 8    ----
            case 0x23: HL += 1;                             break; //INC HL      1 8    ----
            case 0x24: H = INC(H);                          break; //INC H       1 8    Z0H-
            case 0x25: H = DEC(H);                          break; //DEC H       1 8    Z1H-
            case 0x26: LD(ref cycles, IncsType.R8_D8, "H");return cycles;  break;//H = _mmu.Read(PC); PC += 1; ;     break; //LD H,D8     2 8    ----

            case 0x27: //DAA    1 4 Z-0C
                if (FlagN) { // sub
                    if (FlagC) { A -= 0x60; }
                    if (FlagH) { A -= 0x6; }
                } else { // add
                    if (FlagC || (A > 0x99)) { A += 0x60; FlagC = true; }
                    if (FlagH || (A & 0xF) > 0x9) { A += 0x6; }
                }
                SetFlagZ(A);
                FlagH = false;
                break;

            case 0x28: JR(FlagZ);                                 break; //JR Z R8    2 12/8  ----
            case 0x29: DAD(HL);                                        break; //ADD HL,HL  1 8     -0HC
            case 0x2A: LD(ref cycles, IncsType.R8_MAddr, "A", "HL+"); return cycles; return cycles; break;//A = _mmu.Read(HL++);                         break; //LD A (HL+) 1 8     ----
            case 0x2B: HL -= 1;                                        break; //DEC HL     1 4     ----
            case 0x2C: L = INC(L);                                     break; //INC L      1 4     Z0H-
            case 0x2D: L = DEC(L);                                     break; //DEC L      1 4     Z1H-
            case 0x2E: LD(ref cycles, IncsType.R8_D8, "L"); return cycles; break;//L = _mmu.Read(PC); PC += 1; ;                break; //LD L,D8    2 8     ----
            case 0x2F: A = (byte)~A; FlagN = true; FlagH = true;       break; //CPL	       1 4     -11-

            case 0x30: JR(!FlagC);                                break; //JR NC R8   2 12/8  ----
            case 0x31: LD(ref cycles, IncsType.R16_D16, "SP"); return cycles; break;//SP = _mmu.ReadROM16(PC); PC += 2; ;               break; //LD SP,D16  3 12    ----
            case 0x32: LD(ref cycles, IncsType.MAddr_R8, "HL-", "A"); return cycles; break;//_mmu.Write(HL--, A);                         break; //LD (HL-),A 1 8     ----
            case 0x33: SP += 1;                                        break; //INC SP     1 8     ----
            case 0x34: _mmu.Write(HL, INC(_mmu.Read(HL)));       break; //INC (HL)   1 12    Z0H-
            case 0x35: _mmu.Write(HL, DEC(_mmu.Read(HL)));       break; //DEC (HL)   1 12    Z1H-
            case 0x36: LD(ref cycles, IncsType.MAddr_D8); return cycles; break;//_mmu.Write(HL, _mmu.Read(PC)); PC += 1;   break; //LD (HL),D8 2 12    ----
            case 0x37: FlagC = true; FlagN = false; FlagH = false;     break; //SCF	       1 4     -001

            case 0x38: JR(FlagC);                                 break; //JR C R8    2 12/8  ----
            case 0x39: DAD(SP);                                        break; //ADD HL,SP  1 8     -0HC
            case 0x3A: LD(ref cycles, IncsType.R8_MAddr, "A", "HL-"); return cycles; break;//A = _mmu.Read(HL--);                         break; //LD A (HL-) 1 8     ----
            case 0x3B: SP -= 1;                                        break; //DEC SP     1 8     ----
            case 0x3C: A = INC(A);                                     break; //INC A      1 4     Z0H-
            case 0x3D: A = DEC(A);                                     break; //DEC (HL)   1 4     Z1H-
            case 0x3E: 
                A = _mmu.Read(PC); PC += 1;   
                break; //LD A,D8    2 8     ----
            case 0x3F: FlagC = !FlagC; FlagN = false; FlagH = false;   break; //CCF        1 4     -00C

            case 0x40: LD(ref cycles, IncsType.R8_R8, "B", "B"); return cycles; break;///*B = B;*/             break; //LD B,B	    1 4    ----
            case 0x41: LD(ref cycles, IncsType.R8_R8, "B", "C"); return cycles; break;//B = C;                 break; //LD B,C	    1 4	   ----
            case 0x42: LD(ref cycles, IncsType.R8_R8, "B", "D"); return cycles; break;//B = D;                 break; //LD B,D	    1 4	   ----
            case 0x43: LD(ref cycles, IncsType.R8_R8, "B", "E"); return cycles; break;//B = E;                 break; //LD B,E	    1 4	   ----
            case 0x44: LD(ref cycles, IncsType.R8_R8, "B", "H"); return cycles; break;//B = H;                 break; //LD B,H	    1 4	   ----
            case 0x45: LD(ref cycles, IncsType.R8_R8, "B", "L"); return cycles; break;//B = L;                 break; //LD B,L	    1 4	   ----
            case 0x46: LD(ref cycles, IncsType.R8_MAddr, "B", "HL"); return cycles; break;//B = _mmu.Read(HL);  break; //LD B,(HL)	1 8	   ----
            case 0x47: LD(ref cycles, IncsType.R8_R8, "B", "A"); return cycles; break;//B = A;                 break; //LD B,A	    1 4	   ----
                                                
            case 0x48: LD(ref cycles, IncsType.R8_R8, "C", "B");return cycles;//C = B;                 break; //LD C,B	    1 4    ----
            case 0x49: LD(ref cycles, IncsType.R8_R8, "C", "C");return cycles;///*C = C;*/             break; //LD C,C	    1 4    ----
            case 0x4A: LD(ref cycles, IncsType.R8_R8, "C", "D");return cycles;//C = D;                 break; //LD C,D   	1 4    ----
            case 0x4B: LD(ref cycles, IncsType.R8_R8, "C", "E");return cycles;//C = E;                 break; //LD C,E   	1 4    ----
            case 0x4C: LD(ref cycles, IncsType.R8_R8, "C", "H");return cycles;//C = H;                 break; //LD C,H   	1 4    ----
            case 0x4D: LD(ref cycles, IncsType.R8_R8, "C", "L");return cycles;//C = L;                 break; //LD C,L   	1 4    ----
            case 0x4E: LD(ref cycles, IncsType.R8_MAddr, "C", "HL"); return cycles; //C = _mmu.Read(HL);  break; //LD C,(HL)	1 8    ----
            case 0x4F: LD(ref cycles, IncsType.R8_R8, "C", "A");return cycles;//C = A;                 break; //LD C,A   	1 4    ----
                                                                
            case 0x50: LD(ref cycles, IncsType.R8_R8, "D", "B");return cycles;//D = B;                 break; //LD D,B	    1 4    ----
            case 0x51: LD(ref cycles, IncsType.R8_R8, "D", "C");return cycles;//D = C;                 break; //LD D,C	    1 4    ----
            case 0x52: LD(ref cycles, IncsType.R8_R8, "D", "D");return cycles;///*D = D;*/             break; //LD D,D	    1 4    ----
            case 0x53: LD(ref cycles, IncsType.R8_R8, "D", "E");return cycles;//D = E;                 break; //LD D,E	    1 4    ----
            case 0x54: LD(ref cycles, IncsType.R8_R8, "D", "H");return cycles;//D = H;                 break; //LD D,H	    1 4    ----
            case 0x55: LD(ref cycles, IncsType.R8_R8, "D", "L");return cycles;//D = L;                 break; //LD D,L	    1 4    ----
            case 0x56: LD(ref cycles, IncsType.R8_MAddr, "D", "HL"); return cycles;//D = _mmu.Read(HL);  break; //LD D,(HL)    1 8    ---- 
            case 0x57: LD(ref cycles, IncsType.R8_R8, "D", "A");return cycles;//D = A;                 break; //LD D,A	    1 4    ----
                                                                
            case 0x58: LD(ref cycles, IncsType.R8_R8, "E", "B");return cycles;//E = B;                 break; //LD E,B   	1 4    ----
            case 0x59: LD(ref cycles, IncsType.R8_R8, "E", "C");return cycles;//E = C;                 break; //LD E,C   	1 4    ----
            case 0x5A: LD(ref cycles, IncsType.R8_R8, "E", "D");return cycles;//E = D;                 break; //LD E,D   	1 4    ----
            case 0x5B: LD(ref cycles, IncsType.R8_R8, "E", "E");return cycles;///*E = E;*/             break; //LD E,E   	1 4    ----
            case 0x5C: LD(ref cycles, IncsType.R8_R8, "E", "H");return cycles;//E = H;                 break; //LD E,H   	1 4    ----
            case 0x5D: LD(ref cycles, IncsType.R8_R8, "E", "L");return cycles;//E = L;                 break; //LD E,L   	1 4    ----
            case 0x5E: LD(ref cycles, IncsType.R8_MAddr, "E", "HL"); return cycles;//E = _mmu.Read(HL);  break; //LD E,(HL)    1 8    ----
            case 0x5F: LD(ref cycles, IncsType.R8_R8, "E", "A");return cycles;//E = A;                 break; //LD E,A	    1 4    ----
                                                                
            case 0x60: LD(ref cycles, IncsType.R8_R8, "H", "B");return cycles;//H = B;                 break; //LD H,B   	1 4    ----
            case 0x61: LD(ref cycles, IncsType.R8_R8, "H", "C");return cycles;//H = C;                 break; //LD H,C   	1 4    ----
            case 0x62: LD(ref cycles, IncsType.R8_R8, "H", "D");return cycles;//H = D;                 break; //LD H,D   	1 4    ----
            case 0x63: LD(ref cycles, IncsType.R8_R8, "H", "E");return cycles;//H = E;                 break; //LD H,E   	1 4    ----
            case 0x64: LD(ref cycles, IncsType.R8_R8, "H", "H");return cycles;///*H = H;*/             break; //LD H,H   	1 4    ----
            case 0x65: LD(ref cycles, IncsType.R8_R8, "H", "L");return cycles;//H = L;                 break; //LD H,L   	1 4    ----
            case 0x66: LD(ref cycles, IncsType.R8_MAddr, "H", "HL"); return cycles;//H = _mmu.Read(HL);  break; //LD H,(HL)    1 8    ----
            case 0x67: LD(ref cycles, IncsType.R8_R8, "H", "A");return cycles;//H = A;                 break; //LD H,A	    1 4    ----
                                                                
            case 0x68: LD(ref cycles, IncsType.R8_R8, "L", "B");return cycles;//L = B;                 break; //LD L,B   	1 4    ----
            case 0x69: LD(ref cycles, IncsType.R8_R8, "L", "C");return cycles;//L = C;                 break; //LD L,C   	1 4    ----
            case 0x6A: LD(ref cycles, IncsType.R8_R8, "L", "D");return cycles;//L = D;                 break; //LD L,D   	1 4    ----
            case 0x6B: LD(ref cycles, IncsType.R8_R8, "L", "E");return cycles;//L = E;                 break; //LD L,E   	1 4    ----
            case 0x6C: LD(ref cycles, IncsType.R8_R8, "L", "H");return cycles;//L = H;                 break; //LD L,H   	1 4    ----
            case 0x6D: LD(ref cycles, IncsType.R8_R8, "L", "L");return cycles;///*L = L;*/             break; //LD L,L	    1 4    ----
            case 0x6E: LD(ref cycles, IncsType.R8_MAddr, "L", "HL"); return cycles;//L = _mmu.Read(HL);  break; //LD L,(HL)	1 8    ----
            case 0x6F: LD(ref cycles, IncsType.R8_R8, "L", "A");return cycles;//L = A;                 break; //LD L,A	    1 4    ----
                                                
            case 0x70: LD(ref cycles, IncsType.MAddr_R8, "HL", "B"); return cycles;//_mmu.Write(HL, B);  break; //LD (HL),B	1 8    ----
            case 0x71: LD(ref cycles, IncsType.MAddr_R8, "HL", "C"); return cycles;//_mmu.Write(HL, C);  break; //LD (HL),C	1 8	   ----
            case 0x72: LD(ref cycles, IncsType.MAddr_R8, "HL", "D"); return cycles;//_mmu.Write(HL, D);  break; //LD (HL),D	1 8	   ----
            case 0x73: LD(ref cycles, IncsType.MAddr_R8, "HL", "E"); return cycles;//_mmu.Write(HL, E);  break; //LD (HL),E	1 8	   ----
            case 0x74: LD(ref cycles, IncsType.MAddr_R8, "HL", "H"); return cycles;//_mmu.Write(HL, H);  break; //LD (HL),H	1 8	   ----
            case 0x75: LD(ref cycles, IncsType.MAddr_R8, "HL", "L"); return cycles;//_mmu.Write(HL, L);  break; //LD (HL),L	1 8	   ----
            case 0x76: HALT(ref cycles); return cycles;//HALT2();             break; //HLT	        1 4    ----
            case 0x77: LD(ref cycles, IncsType.MAddr_R8, "HL", "A"); return cycles;//_mmu.Write(HL, A);  break; //LD (HL),A	1 8    ----
                                                
            case 0x78: LD(ref cycles, IncsType.R8_R8, "A", "B"); return cycles;//A = B;                 break; //LD A,B	    1 4    ----
            case 0x79: LD(ref cycles, IncsType.R8_R8, "A", "C"); return cycles;//A = C;                 break; //LD A,C	    1 4	   ----
            case 0x7A: LD(ref cycles, IncsType.R8_R8, "A", "D"); return cycles;//A = D;                 break; //LD A,D	    1 4	   ----
            case 0x7B: LD(ref cycles, IncsType.R8_R8, "A", "E"); return cycles;//A = E;                 break; //LD A,E	    1 4	   ----
            case 0x7C: LD(ref cycles, IncsType.R8_R8, "A", "H"); return cycles;//A = H;                 break; //LD A,H	    1 4	   ----
            case 0x7D: LD(ref cycles, IncsType.R8_R8, "A", "L"); return cycles;//A = L;                 break; //LD A,L	    1 4	   ----
            case 0x7E: LD(ref cycles, IncsType.R8_MAddr, "A", "HL");return cycles; //A = _mmu.Read(HL);  break; //LD A,(HL)    1 8    ----
            case 0x7F: LD(ref cycles, IncsType.R8_R8, "A", "A"); return cycles;///*A = A;*/             break; //LD A,A	    1 4    ----

            case 0x80: ADD(ref cycles, IncsType.R8_R8, "B"); return cycles; break;//ADD(B);                break; //ADD B	    1 4    Z0HC	
            case 0x81: ADD(ref cycles, IncsType.R8_R8, "C"); return cycles; break;//ADD(C);                break; //ADD C	    1 4    Z0HC	
            case 0x82: ADD(ref cycles, IncsType.R8_R8, "D"); return cycles; break;//ADD(D);                break; //ADD D	    1 4    Z0HC	
            case 0x83: ADD(ref cycles, IncsType.R8_R8, "E"); return cycles; break;//ADD(E);                break; //ADD E	    1 4    Z0HC	
            case 0x84: ADD(ref cycles, IncsType.R8_R8, "H"); return cycles; break;//ADD(H);                break; //ADD H	    1 4    Z0HC	
            case 0x85: ADD(ref cycles, IncsType.R8_R8, "L"); return cycles; break;//ADD(L);                break; //ADD L	    1 4    Z0HC	
            case 0x86: ADD(ref cycles, IncsType.R8_MAddr); return cycles; break;//ADD(_mmu.Read(HL)); break; //ADD M	    1 8    Z0HC	
            case 0x87: ADD(ref cycles, IncsType.R8_R8, "A"); return cycles; break;//ADD(A);                break; //ADD A	    1 4    Z0HC	
            case 0x88: ADC(ref cycles, IncsType.R8_R8, "B"); return cycles; break;//ADC2(B);                break; //ADC B	    1 4    Z0HC	
            case 0x89: ADC(ref cycles, IncsType.R8_R8, "C"); return cycles; break;//ADC2(C);                break; //ADC C	    1 4    Z0HC	
            case 0x8A: ADC(ref cycles, IncsType.R8_R8, "D"); break;//ADC2(D);                break; //ADC D	    1 4    Z0HC	
            case 0x8B: ADC(ref cycles, IncsType.R8_R8, "E"); break;//ADC2(E);                break; //ADC E	    1 4    Z0HC	
            case 0x8C: ADC(ref cycles, IncsType.R8_R8, "H"); return cycles; break;//ADC2(H);                break; //ADC H	    1 4    Z0HC	
            case 0x8D: ADC(ref cycles, IncsType.R8_R8, "L"); return cycles; break;//ADC2(L);                break; //ADC L	    1 4    Z0HC	
            case 0x8E: ADC(ref cycles, IncsType.R8_MAddr); return cycles; break;//ADC2(_mmu.Read(HL)); break; //ADC M	    1 8    Z0HC	
            case 0x8F: ADC(ref cycles, IncsType.R8_R8, "A"); return cycles; break;//ADC2(A);                break; //ADC A	    1 4    Z0HC	case 0x88: ADC(ref cycles, IncsType.R8_R8, "B"); return cycles; break;//ADC2(B);                break; //ADC B	    1 4    Z0HC	

            case 0x90: SUB(ref cycles, IncsType.R8, "B"); return cycles;//SUB2(B);                break; //SUB B	    1 4    Z1HC
            case 0x91: SUB(ref cycles, IncsType.R8, "C"); return cycles;//SUB2(C);                break; //SUB C	    1 4    Z1HC
            case 0x92: SUB(ref cycles, IncsType.R8, "D"); return cycles;//SUB2(D);                break; //SUB D	    1 4    Z1HC
            case 0x93: SUB(ref cycles, IncsType.R8, "E"); return cycles;//SUB2(E);                break; //SUB E	    1 4    Z1HC
            case 0x94: SUB(ref cycles, IncsType.R8, "H"); return cycles;//SUB2(H);                break; //SUB H	    1 4    Z1HC
            case 0x95: SUB(ref cycles, IncsType.R8, "L"); return cycles;//SUB2(L);                break; //SUB L	    1 4    Z1HC
            case 0x96: SUB(ref cycles, IncsType.MAddr); return cycles;break;//SUB2(_mmu.Read(HL)); break; //SUB M	    1 8    Z1HC
            case 0x97: SUB(ref cycles, IncsType.R8, "A"); return cycles;//SUB2(A);                break; //SUB A	    1 4    Z1HC	    1 4    Z1HC

            case 0x98: SBC(ref cycles, IncsType.R8_R8, "B"); return cycles;//SBC(B);                break; //SBC B	    1 4    Z1HC
            case 0x99: SBC(ref cycles, IncsType.R8_R8, "C"); return cycles;//SBC(C);                break; //SBC C	    1 4    Z1HC
            case 0x9A: SBC(ref cycles, IncsType.R8_R8, "D"); return cycles;//SBC(D);                break; //SBC D	    1 4    Z1HC
            case 0x9B: SBC(ref cycles, IncsType.R8_R8, "E"); return cycles;//SBC(E);                break; //SBC E	    1 4    Z1HC
            case 0x9C: SBC(ref cycles, IncsType.R8_R8, "H"); return cycles;//SBC(H);                break; //SBC H	    1 4    Z1HC
            case 0x9D: SBC(ref cycles, IncsType.R8_R8, "L"); return cycles;//SBC(L);                break; //SBC L	    1 4    Z1HC
            case 0x9E: SBC(ref cycles, IncsType.R8_MAddr); return cycles; //SBC(_mmu.Read(HL)); break; //SBC M	    1 8    Z1HC
            case 0x9F: SBC(ref cycles, IncsType.R8_R8, "A"); return cycles;//SBC(A);                break; //SBC A	    1 4    Z1HC

            case 0xA0: AND(ref cycles, IncsType.R8, "B"); return cycles; //AND(B);                break; //AND B	    1 4    Z010
            case 0xA1: AND(ref cycles, IncsType.R8, "C"); return cycles; //AND(C);                break; //AND C	    1 4    Z010
            case 0xA2: AND(ref cycles, IncsType.R8, "D"); return cycles; //AND(D);                break; //AND D	    1 4    Z010
            case 0xA3: AND(ref cycles, IncsType.R8, "E"); return cycles; //AND(E);                break; //AND E	    1 4    Z010
            case 0xA4: AND(ref cycles, IncsType.R8, "H"); return cycles; //AND(H);                break; //AND H	    1 4    Z010
            case 0xA5: AND(ref cycles, IncsType.R8, "L"); return cycles; //AND(L);                break; //AND L	    1 4    Z010
            case 0xA6: AND(ref cycles, IncsType.MAddr); return cycles; //AND(_mmu.Read(HL)); break; //AND M	    1 8    Z010
            case 0xA7: AND(ref cycles, IncsType.R8, "A"); return cycles; //AND(A);                break; //AND A	    1 4    Z010

            case 0xA8: XOR(ref cycles, IncsType.R8, "B"); return cycles; //XOR(B);                break; //XOR B	    1 4    Z000
            case 0xA9: XOR(ref cycles, IncsType.R8, "C"); return cycles; //XOR(C);                break; //XOR C	    1 4    Z000
            case 0xAA: XOR(ref cycles, IncsType.R8, "D"); return cycles; //XOR(D);                break; //XOR D	    1 4    Z000
            case 0xAB: XOR(ref cycles, IncsType.R8, "E"); return cycles; //XOR(E);                break; //XOR E	    1 4    Z000
            case 0xAC: XOR(ref cycles, IncsType.R8, "H"); return cycles; //XOR(H);                break; //XOR H	    1 4    Z000
            case 0xAD: XOR(ref cycles, IncsType.R8, "L"); return cycles; //XOR(L);                break; //XOR L	    1 4    Z000
            case 0xAE: XOR(ref cycles, IncsType.MAddr); return cycles; //XOR(_mmu.Read(HL)); break; //XOR M	    1 8    Z000
            case 0xAF: XOR(ref cycles, IncsType.R8, "A"); return cycles; //XOR(A);                break; //XOR A	    1 4    Z000

            case 0xB0: OR(ref cycles, IncsType.R8, "B"); return cycles; //OR(B);                 break; //OR B     	1 4    Z000
            case 0xB1: OR(ref cycles, IncsType.R8, "C"); return cycles; //OR(C);                 break; //OR C     	1 4    Z000
            case 0xB2: OR(ref cycles, IncsType.R8, "D"); return cycles; //OR(D);                 break; //OR D     	1 4    Z000
            case 0xB3: OR(ref cycles, IncsType.R8, "E"); return cycles; //OR(E);                 break; //OR E     	1 4    Z000
            case 0xB4: OR(ref cycles, IncsType.R8, "H"); return cycles; //OR(H);                 break; //OR H     	1 4    Z000
            case 0xB5: OR(ref cycles, IncsType.R8, "L"); return cycles; //OR(L);                 break; //OR L     	1 4    Z000
            case 0xB6: OR(ref cycles, IncsType.MAddr); return cycles; //OR(_mmu.Read(HL));  break; //OR M     	1 8    Z000
            case 0xB7: OR(ref cycles, IncsType.R8, "A"); return cycles; //OR(A);                 break; //OR A     	1 4    Z000

            case 0xB8: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(B);                 break; //CP B     	1 4    Z1HC
            case 0xB9: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(C);                 break; //CP C     	1 4    Z1HC
            case 0xBA: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(D);                 break; //CP D     	1 4    Z1HC
            case 0xBB: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(E);                 break; //CP E     	1 4    Z1HC
            case 0xBC: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(H);                 break; //CP H     	1 4    Z1HC
            case 0xBD: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(L);                 break; //CP L     	1 4    Z1HC
            case 0xBE: CP(ref cycles, IncsType.MAddr); return cycles;// CP(_mmu.Read(HL));  break; //CP M     	1 8    Z1HC
            case 0xBF: CP(ref cycles, IncsType.R8, "B"); return cycles; //CP(A);                 break; //CP A     	1 4    Z1HC

            case 0xC0: RET(ref cycles, !FlagZ); return cycles; //RETURN(!FlagZ);             break; //RET NZ	     1 20/8  ----
            case 0xC1: POP(ref cycles, "BC"); return cycles; //BC = POP();                   break; //POP BC      1 12    ----
            case 0xC2: JP(ref cycles, !FlagZ); return cycles; //JUMP(!FlagZ);               break; //JP NZ,A16   3 16/12 ----
            case 0xC3: JP(ref cycles, true); return cycles; //JUMP(true);                 break; //JP A16      3 16    ----
            case 0xC4: CALL(!FlagZ);               break; //CALL NZ A16 3 24/12 ----
            case 0xC5: PUSH(BC);                   break; //PUSH BC     1 16    ----
            case 0xC6: ADD(ref cycles, IncsType.R8_D8); return cycles; break;//ADD(_mmu.Read(PC)); PC += 1;  break; //ADD A,D8    2 8     Z0HC
            case 0xC7: RST(0x0);                   break; //RST 0       1 16    ----

            case 0xC8: RET(ref cycles, FlagZ); return cycles;//RETURN(FlagZ);              break; //RET Z       1 20/8  ----
            case 0xC9: RET(ref cycles, true); return cycles;// RETURN(true);               break; //RET         1 16    ----
            case 0xCA: JP(ref cycles, FlagZ); return cycles; //               break; //JP Z,A16    3 16/12 ----
            case 0xCB: PREFIX_CB(_mmu.Read(PC++));      break; //PREFIX CB OPCODE TABLE
            case 0xCC: CALL(FlagZ);                break; //CALL Z,A16  3 24/12 ----
            case 0xCD: CALL(true);                 break; //CALL A16    3 24    ----
            case 0xCE: ADC(ref cycles, IncsType.R8_D8); return cycles; break;//ADC(_mmu.Read(PC)); PC += 1;  break; //ADC A,D8    2 8     ----
            case 0xCF: RST(0x8);                   break; //RST 1 08    1 16    ----

            case 0xD0: RET(ref cycles, !FlagC); return cycles;//RETURN(!FlagC);             break; //RET NC      1 20/8  ----
            case 0xD1: POP(ref cycles, "DE"); return cycles; //DE = POP();                   break; //POP DE      1 12    ----
            case 0xD2: JP(ref cycles, !FlagC); return cycles; //JUMP(!FlagC);               break; //JP NC,A16   3 16/12 ----
            //case 0xD3:                                break; //Illegal Opcode
            case 0xD4: CALL(!FlagC);               break; //CALL NC,A16 3 24/12 ----
            case 0xD5: PUSH(DE);                   break; //PUSH DE     1 16    ----
            case 0xD6: SUB(ref cycles, IncsType.D8); return cycles;break;//SUB(_mmu.Read(PC)); PC += 1;  break; //SUB D8      2 8     ----
            case 0xD7: RST(0x10);                  break; //RST 2 10    1 16    ----

            case 0xD8: RET(ref cycles, FlagC); return cycles;//RETURN(FlagC);              break; //RET C       1 20/8  ----
            case 0xD9: RETI(ref cycles); return cycles; //RETURN(true); IME = true;   break; //RETI        1 16    ----
            case 0xDA: JP(ref cycles, FlagC); return cycles; //JUMP(FlagC);                break; //JP C,A16    3 16/12 ----
            //case 0xDB:                                break; //Illegal Opcode
            case 0xDC: CALL(FlagC);                break; //Call C,A16  3 24/12 ----
            //case 0xDD:                                break; //Illegal Opcode
            case 0xDE: SBC(ref cycles, IncsType.R8_D8); return cycles;//SBC(_mmu.Read(PC)); PC += 1;  break; //SBC A,A8    2 8     Z1HC
            case 0xDF: RST(0x18);                  break; //RST 3 18    1 16    ----

            case 0xE0:  //LDH (A8),A 2 12 ----
                ushort value1 = (ushort)(0xFF00 + _mmu.Read(PC));
                byte value2 = A;
                _mmu.Write((ushort)(0xFF00 + _mmu.Read(PC)), A); 
                PC += 1;  
                break;
            case 0xE1: POP(ref cycles, "HL"); return cycles; //HL = POP();                   break; //POP HL      1 12    ----
            case 0xE2: _mmu.Write((ushort)(0xFF00 + C), A);   break; //LD (C),A   1 8  ----
            //case 0xE3:                                break; //Illegal Opcode
            //case 0xE4:                                break; //Illegal Opcode
            case 0xE5: PUSH(HL);                   break; //PUSH HL     1 16    ----
            case 0xE6: AND(ref cycles, IncsType.D8); return cycles;//AND(_mmu.Read(PC)); PC += 1;  break; //AND D8      2 8     Z010
            case 0xE7: RST(0x20);                  break; //RST 4 20    1 16    ----

            case 0xE8: SP = DADr8(SP);             break; //ADD SP,R8   2 16    00HC
            case 0xE9: PC = HL;                         break; //JP (HL)     1 4     ----
            case 0xEA: _mmu.Write(_mmu.ReadROM16(PC), A); PC += 2;                     break; //LD (A16),A 3 16 ----
            //case 0xEB:                                break; //Illegal Opcode
            //case 0xEC:                                break; //Illegal Opcode
            //case 0xED:                                break; //Illegal Opcode
            case 0xEE: XOR(ref cycles, IncsType.D8); return cycles; //XOR2(_mmu.Read(PC)); PC += 1;  break; //XOR D8      2 8     Z000
            case 0xEF: RST(0x28);                  break; //RST 5 28    1 16    ----

            case 0xF0: A = _mmu.Read((ushort)(0xFF00 + _mmu.Read(PC))); PC += 1;  break; //LDH A,(A8)  2 12    ----
            case 0xF1: POP(ref cycles, "AF"); return cycles; //AF = POP();                   break; //POP AF      1 12    ZNHC
            case 0xF2: A = _mmu.Read((ushort)(0xFF00 + C));  break; //LD A,(C)    1 8     ----
            case 0xF3: IME = false;                     break; //DI          1 4     ----
            //case 0xF4:                                break; //Illegal Opcode
            case 0xF5: PUSH(AF);                   break; //PUSH AF     1 16    ----
            case 0xF6: OR(ref cycles, IncsType.D8); return cycles; //OR(_mmu.Read(PC)); PC += 1;   break; //OR D8       2 8     Z000
            case 0xF7: RST(0x30);                  break; //RST 6 30    1 16    ----

            case 0xF8: HL = DADr8(SP);             break; //LD HL,SP+R8 2 12    00HC
            case 0xF9: SP = HL;                         break; //LD SP,HL    1 8     ----
            case 0xFA: A = _mmu.Read(_mmu.ReadROM16(PC)); PC += 2;   break; //LD A,(A16)  3 16    ----
            case 0xFB: IMEEnabler = true;               break; //IE          1 4     ----
            //case 0xFC:                                break; //Illegal Opcode
            //case 0xFD:                                break; //Illegal Opcode
            case 0xFE: CP(ref cycles, IncsType.D8); return cycles; //CP(_mmu.Read(PC)); PC += 1;   break; //CP D8       2 8     Z1HC
            case 0xFF: RST(0x38);                  break; //RST 7 38    1 16    ----

            default: warnUnsupportedOpcode(opcode);     break;
        }
        cycles += _incCycles[opcode];
        return cycles;
    }
    private void LD(ref int _cycles, IncsType incsType, string data1 = "", string data2 = "")
    {
        if (incsType == IncsType.R16_D16)
        {
            switch (data1)
            {
                case "BC": BC = _mmu.ReadROM16(PC); break;
                case "DE": DE = _mmu.ReadROM16(PC); break;
                case "HL": HL = _mmu.ReadROM16(PC); break;
                case "SP": SP = _mmu.ReadROM16(PC); break;
            }
            PC += 2; 
            _cycles += 12;
        }
        else if (incsType == IncsType.MAddr_R8)
        {
            u16 address = 0;
            u8 value = 0;
            switch (data1)
            {
                case "BC": address = BC; break;
                case "DE": address = DE; break;
                case "HL": address = HL; break;
                case "HL+": address = HL; HL ++; break;
                case "HL-": address = HL; HL --; break;
            }

            switch (data2)
            {
                case "A": value = A; break;
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
            }

            _mmu.Write(address, value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u16 t1 = PC;
            u8 t2 = _mmu.Read( PC);
            switch (data1)
            {
                case "B": B = _mmu.Read(PC ++); break;
                case "D": D = _mmu.Read(PC ++); break;
                case "H": H = _mmu.Read(PC ++); break;
                case "C": C = _mmu.Read(PC ++); break;
                case "E": E = _mmu.Read(PC ++); break;
                case "L": L = _mmu.Read(PC ++); break;
                case "A": A = _mmu.Read(PC ++); break;
            }
            u8 t3 = A;
            _cycles += 8;
        }
        else if (incsType == IncsType.A16_R16)
        {
            _mmu.WriteROM16(_mmu.ReadROM16(PC), SP);
            PC += 2;
            _cycles += 20;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = 0;
            switch (data2)
            {
                case "BC": value = _mmu.Read(BC); break;
                case "DE": value = _mmu.Read(DE); break;
                case "HL+": value = _mmu.Read(HL ++); break;
                case "HL-": value = _mmu.Read(HL --); break;
                case "HL": value = _mmu.Read(HL); break;
            }
            
            switch (data1)
            {
                case "A": A = value; break;
                case "B": B = value; break;
                case "C": C = value; break;
                case "D": D = value; break;
                case "E": E = value; break;
                case "H": H = value; break;
                case "L": L = value; break;
            }
            
            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr_D8)
        {
            _mmu.Write(HL, _mmu.Read(PC ++));
            _cycles += 12;
        }
        else if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data2)
            {
                case "A": value = A; break;
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "L": value = L; break;
                case "H": value = H; break;
            }

            switch (data1)
            {
                case "A": A = value; break;
                case "B": B = value; break;
                case "C": C = value; break;
                case "D": D = value; break;
                case "E": E = value; break;
                case "H": H = value; break;
                case "L": L = value; break;
            }

            _cycles += 4;
        }
        else if (incsType == IncsType.A8_R8)
        {
            u16 value1 = (u16) (0xFF00 + _mmu.Read(PC));
            u8 value2 = A;
            _mmu.Write((u16) (0xFF00 + _mmu.Read(PC ++)), A);
            _cycles += 12;
        }
        else if (incsType == IncsType.R8_A8)
        {
            A = _mmu.Read((u16) (0xFF00 + _mmu.Read(PC ++)));
            _cycles += 12;
        }
        else if (incsType == IncsType.MAddr8_R8)
        {
            _mmu.Write((u16) (0xFF00 + C), A);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_MAddr8)
        {
            A = _mmu.Read((u16) (0xFF00 + C));
            _cycles += 8;
        }
        else if (incsType == IncsType.R16_R16S8)
        {
            u8 value = _mmu.Read(PC ++);
            // SetFlag(Registers.Flag.Z, false);
            // FlagN = false;
            FlagZ = false;
            FlagN = false;
            SetFlagH((u8) SP, value);
            SetFlagC((u8) SP + value);
            HL = (u16) (SP + (sbyte) value);
            _cycles += 12;
        }
        else if (incsType == IncsType.R16_R16)
        {
            SP = HL;
            _cycles += 8;
        }
        else if (incsType == IncsType.A16_R8)
        {
            _mmu.Write(_mmu.ReadROM16(PC), A);
            PC += 2;
            _cycles += 16;
        }
        else if (incsType == IncsType.R8_A16)
        {
            A = _mmu.Read(_mmu.ReadROM16(PC));
            PC += 2;
            _cycles += 16;
        }
    }

    private int[] _cbIncCycles = {
        //0 1 2 3 4 5  6 7 8 9 A B C D  E  F
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //0
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //1
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //2
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //3
                                            
        8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8, //4
        8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8, //5
        8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8, //6
        8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8, //7
                                            
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //8
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //9
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //A
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //B
                                            
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //C
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //D
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8, //E
        8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8  //F
    };

    private void PREFIX_CB(byte opcode) {
        switch (opcode) {
            case 0x00: B = RLC(B);                                  break; //RLC B    2   8   Z00C
            case 0x01: C = RLC(C);                                  break; //RLC C    2   8   Z00C
            case 0x02: D = RLC(D);                                  break; //RLC D    2   8   Z00C
            case 0x03: E = RLC(E);                                  break; //RLC E    2   8   Z00C
            case 0x04: H = RLC(H);                                  break; //RLC H    2   8   Z00C
            case 0x05: L = RLC(L);                                  break; //RLC L    2   8   Z00C
            case 0x06: _mmu.Write(HL, RLC(_mmu.Read(HL)));    break; //RLC (HL) 2   8   Z00C
            case 0x07: A = RLC(A);                                  break; //RLC B    2   8   Z00C
                                                                    
            case 0x08: B = RRC(B);                                  break; //RRC B    2   8   Z00C
            case 0x09: C = RRC(C);                                  break; //RRC C    2   8   Z00C
            case 0x0A: D = RRC(D);                                  break; //RRC D    2   8   Z00C
            case 0x0B: E = RRC(E);                                  break; //RRC E    2   8   Z00C
            case 0x0C: H = RRC(H);                                  break; //RRC H    2   8   Z00C
            case 0x0D: L = RRC(L);                                  break; //RRC L    2   8   Z00C
            case 0x0E: _mmu.Write(HL, RRC(_mmu.Read(HL)));    break; //RRC (HL) 2   8   Z00C
            case 0x0F: A = RRC(A);                                  break; //RRC B    2   8   Z00C
                                                                        
            case 0x10: B = RL(B);                                   break; //RL B     2   8   Z00C
            case 0x11: C = RL(C);                                   break; //RL C     2   8   Z00C
            case 0x12: D = RL(D);                                   break; //RL D     2   8   Z00C
            case 0x13: E = RL(E);                                   break; //RL E     2   8   Z00C
            case 0x14: H = RL(H);                                   break; //RL H     2   8   Z00C
            case 0x15: L = RL(L);                                   break; //RL L     2   8   Z00C
            case 0x16: _mmu.Write(HL, RL(_mmu.Read(HL)));     break; //RL (HL)  2   8   Z00C
            case 0x17: A = RL(A);                                   break; //RL B     2   8   Z00C
                                                                                            
            case 0x18: B = RR(B);                                   break; //RR B     2   8   Z00C
            case 0x19: C = RR(C);                                   break; //RR C     2   8   Z00C
            case 0x1A: D = RR(D);                                   break; //RR D     2   8   Z00C
            case 0x1B: E = RR(E);                                   break; //RR E     2   8   Z00C
            case 0x1C: H = RR(H);                                   break; //RR H     2   8   Z00C
            case 0x1D: L = RR(L);                                   break; //RR L     2   8   Z00C
            case 0x1E: _mmu.Write(HL, RR(_mmu.Read(HL)));     break; //RR (HL)  2   8   Z00C
            case 0x1F: A = RR(A);                                   break; //RR B     2   8   Z00C
                                                                        
            case 0x20: B = SLA(B);                                  break; //SLA B    2   8   Z00C
            case 0x21: C = SLA(C);                                  break; //SLA C    2   8   Z00C
            case 0x22: D = SLA(D);                                  break; //SLA D    2   8   Z00C
            case 0x23: E = SLA(E);                                  break; //SLA E    2   8   Z00C
            case 0x24: H = SLA(H);                                  break; //SLA H    2   8   Z00C
            case 0x25: L = SLA(L);                                  break; //SLA L    2   8   Z00C
            case 0x26: _mmu.Write(HL, SLA(_mmu.Read(HL)));    break; //SLA (HL) 2   8   Z00C
            case 0x27: A = SLA(A);                                  break; //SLA B    2   8   Z00C
                                                                        
            case 0x28: B = SRA(B);                                  break; //SRA B    2   8   Z00C
            case 0x29: C = SRA(C);                                  break; //SRA C    2   8   Z00C
            case 0x2A: D = SRA(D);                                  break; //SRA D    2   8   Z00C
            case 0x2B: E = SRA(E);                                  break; //SRA E    2   8   Z00C
            case 0x2C: H = SRA(H);                                  break; //SRA H    2   8   Z00C
            case 0x2D: L = SRA(L);                                  break; //SRA L    2   8   Z00C
            case 0x2E: _mmu.Write(HL, SRA(_mmu.Read(HL)));    break; //SRA (HL) 2   8   Z00C
            case 0x2F: A = SRA(A);                                  break; //SRA B    2   8   Z00C
                                                                        
            case 0x30: B = SWAP(B);                                 break; //SWAP B    2   8   Z00C
            case 0x31: C = SWAP(C);                                 break; //SWAP C    2   8   Z00C
            case 0x32: D = SWAP(D);                                 break; //SWAP D    2   8   Z00C
            case 0x33: E = SWAP(E);                                 break; //SWAP E    2   8   Z00C
            case 0x34: H = SWAP(H);                                 break; //SWAP H    2   8   Z00C
            case 0x35: L = SWAP(L);                                 break; //SWAP L    2   8   Z00C
            case 0x36: _mmu.Write(HL, SWAP(_mmu.Read(HL)));   break; //SWAP (HL) 2   8   Z00C
            case 0x37: A = SWAP(A);                                 break; //SWAP B    2   8   Z00C
                                                                        
            case 0x38: B = SRL(B);                                  break; //SRL B    2   8   Z000
            case 0x39: C = SRL(C);                                  break; //SRL C    2   8   Z000
            case 0x3A: D = SRL(D);                                  break; //SRL D    2   8   Z000
            case 0x3B: E = SRL(E);                                  break; //SRL E    2   8   Z000
            case 0x3C: H = SRL(H);                                  break; //SRL H    2   8   Z000
            case 0x3D: L = SRL(L);                                  break; //SRL L    2   8   Z000
            case 0x3E: _mmu.Write(HL, SRL(_mmu.Read(HL)));    break; //SRL (HL) 2   8   Z000
            case 0x3F: A = SRL(A);                                  break; //SRL B    2   8   Z000

            case 0x40: BIT(0x1, B);                                 break; //BIT B    2   8   Z01-
            case 0x41: BIT(0x1, C);                                 break; //BIT C    2   8   Z01-
            case 0x42: BIT(0x1, D);                                 break; //BIT D    2   8   Z01-
            case 0x43: BIT(0x1, E);                                 break; //BIT E    2   8   Z01-
            case 0x44: BIT(0x1, H);                                 break; //BIT H    2   8   Z01-
            case 0x45: BIT(0x1, L);                                 break; //BIT L    2   8   Z01-
            case 0x46: BIT(0x1, _mmu.Read(HL));                  break; //BIT (HL) 2   8   Z01-
            case 0x47: BIT(0x1, A);                                 break; //BIT B    2   8   Z01-

            case 0x48: BIT(0x2, B);                                break; //BIT B    2   8   Z01-
            case 0x49: BIT(0x2, C);                                break; //BIT C    2   8   Z01-
            case 0x4A: BIT(0x2, D);                                break; //BIT D    2   8   Z01-
            case 0x4B: BIT(0x2, E);                                break; //BIT E    2   8   Z01-
            case 0x4C: BIT(0x2, H);                                break; //BIT H    2   8   Z01-
            case 0x4D: BIT(0x2, L);                                break; //BIT L    2   8   Z01-
            case 0x4E: BIT(0x2, _mmu.Read(HL));                 break; //BIT (HL) 2   8   Z01-
            case 0x4F: BIT(0x2, A);                                break; //BIT B    2   8   Z01-
                                                                    
            case 0x50: BIT(0x4, B);                                break; //BIT B    2   8   Z01-
            case 0x51: BIT(0x4, C);                                break; //BIT C    2   8   Z01-
            case 0x52: BIT(0x4, D);                                break; //BIT D    2   8   Z01-
            case 0x53: BIT(0x4, E);                                break; //BIT E    2   8   Z01-
            case 0x54: BIT(0x4, H);                                break; //BIT H    2   8   Z01-
            case 0x55: BIT(0x4, L);                                break; //BIT L    2   8   Z01-
            case 0x56: BIT(0x4, _mmu.Read(HL));                 break; //BIT (HL) 2   8   Z01-
            case 0x57: BIT(0x4, A);                                break; //BIT B    2   8   Z01-

            case 0x58: BIT(0x8, B);                                break; //BIT B    2   8   Z01-
            case 0x59: BIT(0x8, C);                                break; //BIT C    2   8   Z01-
            case 0x5A: BIT(0x8, D);                                break; //BIT D    2   8   Z01-
            case 0x5B: BIT(0x8, E);                                break; //BIT E    2   8   Z01-
            case 0x5C: BIT(0x8, H);                                break; //BIT H    2   8   Z01-
            case 0x5D: BIT(0x8, L);                                break; //BIT L    2   8   Z01-
            case 0x5E: BIT(0x8, _mmu.Read(HL));                 break; //BIT (HL) 2   8   Z01-
            case 0x5F: BIT(0x8, A);                                break; //BIT B    2   8   Z01-

            case 0x60: BIT(0x10, B);                               break; //BIT B    2   8   Z01-
            case 0x61: BIT(0x10, C);                               break; //BIT C    2   8   Z01-
            case 0x62: BIT(0x10, D);                               break; //BIT D    2   8   Z01-
            case 0x63: BIT(0x10, E);                               break; //BIT E    2   8   Z01-
            case 0x64: BIT(0x10, H);                               break; //BIT H    2   8   Z01-
            case 0x65: BIT(0x10, L);                               break; //BIT L    2   8   Z01-
            case 0x66: BIT(0x10, _mmu.Read(HL));                break; //BIT (HL) 2   8   Z01-
            case 0x67: BIT(0x10, A);                               break; //BIT B    2   8   Z01-

            case 0x68: BIT(0x20, B);                               break; //BIT B    2   8   Z01-
            case 0x69: BIT(0x20, C);                               break; //BIT C    2   8   Z01-
            case 0x6A: BIT(0x20, D);                               break; //BIT D    2   8   Z01-
            case 0x6B: BIT(0x20, E);                               break; //BIT E    2   8   Z01-
            case 0x6C: BIT(0x20, H);                               break; //BIT H    2   8   Z01-
            case 0x6D: BIT(0x20, L);                               break; //BIT L    2   8   Z01-
            case 0x6E: BIT(0x20, _mmu.Read(HL));                break; //BIT (HL) 2   8   Z01-
            case 0x6F: BIT(0x20, A);                               break; //BIT B    2   8   Z01-

            case 0x70: BIT(0x40, B);                               break; //BIT B    2   8   Z01-
            case 0x71: BIT(0x40, C);                               break; //BIT C    2   8   Z01-
            case 0x72: BIT(0x40, D);                               break; //BIT D    2   8   Z01-
            case 0x73: BIT(0x40, E);                               break; //BIT E    2   8   Z01-
            case 0x74: BIT(0x40, H);                               break; //BIT H    2   8   Z01-
            case 0x75: BIT(0x40, L);                               break; //BIT L    2   8   Z01-
            case 0x76: BIT(0x40, _mmu.Read(HL));                break; //BIT (HL) 2   8   Z01-
            case 0x77: BIT(0x40, A);                               break; //BIT B    2   8   Z01-

            case 0x78: BIT(0x80, B);                               break; //BIT B    2   8   Z01-
            case 0x79: BIT(0x80, C);                               break; //BIT C    2   8   Z01-
            case 0x7A: BIT(0x80, D);                               break; //BIT D    2   8   Z01-
            case 0x7B: BIT(0x80, E);                               break; //BIT E    2   8   Z01-
            case 0x7C: BIT(0x80, H);                               break; //BIT H    2   8   Z01-
            case 0x7D: BIT(0x80, L);                               break; //BIT L    2   8   Z01-
            case 0x7E: BIT(0x80, _mmu.Read(HL));                break; //BIT (HL) 2   8   Z01-
            case 0x7F: BIT(0x80, A);                               break; //BIT B    2   8   Z01-

            case 0x80: B = RES(0x1, B);                               break; //RES B    2   8   ----
            case 0x81: C = RES(0x1, C);                               break; //RES C    2   8   ----
            case 0x82: D = RES(0x1, D);                               break; //RES D    2   8   ----
            case 0x83: E = RES(0x1, E);                               break; //RES E    2   8   ----
            case 0x84: H = RES(0x1, H);                               break; //RES H    2   8   ----
            case 0x85: L = RES(0x1, L);                               break; //RES L    2   8   ----
            case 0x86: _mmu.Write(HL, RES(0x1, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0x87: A = RES(0x1, A);                               break; //RES B    2   8   ----

            case 0x88: B = RES(0x2, B);                               break; //RES B    2   8   ----
            case 0x89: C = RES(0x2, C);                               break; //RES C    2   8   ----
            case 0x8A: D = RES(0x2, D);                               break; //RES D    2   8   ----
            case 0x8B: E = RES(0x2, E);                               break; //RES E    2   8   ----
            case 0x8C: H = RES(0x2, H);                               break; //RES H    2   8   ----
            case 0x8D: L = RES(0x2, L);                               break; //RES L    2   8   ----
            case 0x8E: _mmu.Write(HL, RES(0x2, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0x8F: A = RES(0x2, A);                               break; //RES B    2   8   ----

            case 0x90: B = RES(0x4, B);                               break; //RES B    2   8   ----
            case 0x91: C = RES(0x4, C);                               break; //RES C    2   8   ----
            case 0x92: D = RES(0x4, D);                               break; //RES D    2   8   ----
            case 0x93: E = RES(0x4, E);                               break; //RES E    2   8   ----
            case 0x94: H = RES(0x4, H);                               break; //RES H    2   8   ----
            case 0x95: L = RES(0x4, L);                               break; //RES L    2   8   ----
            case 0x96: _mmu.Write(HL, RES(0x4, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0x97: A = RES(0x4, A);                               break; //RES B    2   8   ----

            case 0x98: B = RES(0x8, B);                               break; //RES B    2   8   ----
            case 0x99: C = RES(0x8, C);                               break; //RES C    2   8   ----
            case 0x9A: D = RES(0x8, D);                               break; //RES D    2   8   ----
            case 0x9B: E = RES(0x8, E);                               break; //RES E    2   8   ----
            case 0x9C: H = RES(0x8, H);                               break; //RES H    2   8   ----
            case 0x9D: L = RES(0x8, L);                               break; //RES L    2   8   ----
            case 0x9E: _mmu.Write(HL, RES(0x8, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0x9F: A = RES(0x8, A);                               break; //RES B    2   8   ----

            case 0xA0: B = RES(0x10, B);                               break; //RES B    2   8   ----
            case 0xA1: C = RES(0x10, C);                               break; //RES C    2   8   ----
            case 0xA2: D = RES(0x10, D);                               break; //RES D    2   8   ----
            case 0xA3: E = RES(0x10, E);                               break; //RES E    2   8   ----
            case 0xA4: H = RES(0x10, H);                               break; //RES H    2   8   ----
            case 0xA5: L = RES(0x10, L);                               break; //RES L    2   8   ----
            case 0xA6: _mmu.Write(HL, RES(0x10, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0xA7: A = RES(0x10, A);                               break; //RES B    2   8   ----

            case 0xA8: B = RES(0x20, B);                               break; //RES B    2   8   ----
            case 0xA9: C = RES(0x20, C);                               break; //RES C    2   8   ----
            case 0xAA: D = RES(0x20, D);                               break; //RES D    2   8   ----
            case 0xAB: E = RES(0x20, E);                               break; //RES E    2   8   ----
            case 0xAC: H = RES(0x20, H);                               break; //RES H    2   8   ----
            case 0xAD: L = RES(0x20, L);                               break; //RES L    2   8   ----
            case 0xAE: _mmu.Write(HL, RES(0x20, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0xAF: A = RES(0x20, A);                               break; //RES B    2   8   ----

            case 0xB0: B = RES(0x40, B);                               break; //RES B    2   8   ----
            case 0xB1: C = RES(0x40, C);                               break; //RES C    2   8   ----
            case 0xB2: D = RES(0x40, D);                               break; //RES D    2   8   ----
            case 0xB3: E = RES(0x40, E);                               break; //RES E    2   8   ----
            case 0xB4: H = RES(0x40, H);                               break; //RES H    2   8   ----
            case 0xB5: L = RES(0x40, L);                               break; //RES L    2   8   ----
            case 0xB6: _mmu.Write(HL, RES(0x40, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0xB7: A = RES(0x40, A);                               break; //RES B    2   8   ----

            case 0xB8: B = RES(0x80, B);                               break; //RES B    2   8   ----
            case 0xB9: C = RES(0x80, C);                               break; //RES C    2   8   ----
            case 0xBA: D = RES(0x80, D);                               break; //RES D    2   8   ----
            case 0xBB: E = RES(0x80, E);                               break; //RES E    2   8   ----
            case 0xBC: H = RES(0x80, H);                               break; //RES H    2   8   ----
            case 0xBD: L = RES(0x80, L);                               break; //RES L    2   8   ----
            case 0xBE: _mmu.Write(HL, RES(0x80, _mmu.Read(HL))); break; //RES (HL) 2   8   ----
            case 0xBF: A = RES(0x80, A);                               break; //RES B    2   8   ----

            case 0xC0: B = SET(0x1, B);                               break; //SET B    2   8   ----
            case 0xC1: C = SET(0x1, C);                               break; //SET C    2   8   ----
            case 0xC2: D = SET(0x1, D);                               break; //SET D    2   8   ----
            case 0xC3: E = SET(0x1, E);                               break; //SET E    2   8   ----
            case 0xC4: H = SET(0x1, H);                               break; //SET H    2   8   ----
            case 0xC5: L = SET(0x1, L);                               break; //SET L    2   8   ----
            case 0xC6: _mmu.Write(HL, SET(0x1, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xC7: A = SET(0x1, A);                               break; //SET B    2   8   ----

            case 0xC8: B = SET(0x2, B);                               break; //SET B    2   8   ----
            case 0xC9: C = SET(0x2, C);                               break; //SET C    2   8   ----
            case 0xCA: D = SET(0x2, D);                               break; //SET D    2   8   ----
            case 0xCB: E = SET(0x2, E);                               break; //SET E    2   8   ----
            case 0xCC: H = SET(0x2, H);                               break; //SET H    2   8   ----
            case 0xCD: L = SET(0x2, L);                               break; //SET L    2   8   ----
            case 0xCE: _mmu.Write(HL, SET(0x2, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xCF: A = SET(0x2, A);                               break; //SET B    2   8   ----

            case 0xD0: B = SET(0x4, B);                               break; //SET B    2   8   ----
            case 0xD1: C = SET(0x4, C);                               break; //SET C    2   8   ----
            case 0xD2: D = SET(0x4, D);                               break; //SET D    2   8   ----
            case 0xD3: E = SET(0x4, E);                               break; //SET E    2   8   ----
            case 0xD4: H = SET(0x4, H);                               break; //SET H    2   8   ----
            case 0xD5: L = SET(0x4, L);                               break; //SET L    2   8   ----
            case 0xD6: _mmu.Write(HL, SET(0x4, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xD7: A = SET(0x4, A);                               break; //SET B    2   8   ----

            case 0xD8: B = SET(0x8, B);                               break; //SET B    2   8   ----
            case 0xD9: C = SET(0x8, C);                               break; //SET C    2   8   ----
            case 0xDA: D = SET(0x8, D);                               break; //SET D    2   8   ----
            case 0xDB: E = SET(0x8, E);                               break; //SET E    2   8   ----
            case 0xDC: H = SET(0x8, H);                               break; //SET H    2   8   ----
            case 0xDD: L = SET(0x8, L);                               break; //SET L    2   8   ----
            case 0xDE: _mmu.Write(HL, SET(0x8, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xDF: A = SET(0x8, A);                               break; //SET B    2   8   ----

            case 0xE0: B = SET(0x10, B);                               break; //SET B    2   8   ----
            case 0xE1: C = SET(0x10, C);                               break; //SET C    2   8   ----
            case 0xE2: D = SET(0x10, D);                               break; //SET D    2   8   ----
            case 0xE3: E = SET(0x10, E);                               break; //SET E    2   8   ----
            case 0xE4: H = SET(0x10, H);                               break; //SET H    2   8   ----
            case 0xE5: L = SET(0x10, L);                               break; //SET L    2   8   ----
            case 0xE6: _mmu.Write(HL, SET(0x10, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xE7: A = SET(0x10, A);                               break; //SET B    2   8   ----

            case 0xE8: B = SET(0x20, B);                               break; //SET B    2   8   ----
            case 0xE9: C = SET(0x20, C);                               break; //SET C    2   8   ----
            case 0xEA: D = SET(0x20, D);                               break; //SET D    2   8   ----
            case 0xEB: E = SET(0x20, E);                               break; //SET E    2   8   ----
            case 0xEC: H = SET(0x20, H);                               break; //SET H    2   8   ----
            case 0xED: L = SET(0x20, L);                               break; //SET L    2   8   ----
            case 0xEE: _mmu.Write(HL, SET(0x20, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xEF: A = SET(0x20, A);                               break; //SET B    2   8   ----

            case 0xF0: B = SET(0x40, B);                               break; //SET B    2   8   ----
            case 0xF1: C = SET(0x40, C);                               break; //SET C    2   8   ----
            case 0xF2: D = SET(0x40, D);                               break; //SET D    2   8   ----
            case 0xF3: E = SET(0x40, E);                               break; //SET E    2   8   ----
            case 0xF4: H = SET(0x40, H);                               break; //SET H    2   8   ----
            case 0xF5: L = SET(0x40, L);                               break; //SET L    2   8   ----
            case 0xF6: _mmu.Write(HL, SET(0x40, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xF7: A = SET(0x40, A);                               break; //SET B    2   8   ----

            case 0xF8: B = SET(0x80, B);                               break; //SET B    2   8   ----
            case 0xF9: C = SET(0x80, C);                               break; //SET C    2   8   ----
            case 0xFA: D = SET(0x80, D);                               break; //SET D    2   8   ----
            case 0xFB: E = SET(0x80, E);                               break; //SET E    2   8   ----
            case 0xFC: H = SET(0x80, H);                               break; //SET H    2   8   ----
            case 0xFD: L = SET(0x80, L);                               break; //SET L    2   8   ----
            case 0xFE: _mmu.Write(HL, SET(0x80, _mmu.Read(HL))); break; //SET (HL) 2   8   ----
            case 0xFF: A = SET(0x80, A);                               break; //SET B    2   8   ----

            default: warnUnsupportedOpcode(opcode); break;
        }
        cycles += _cbIncCycles[opcode];
    }

    private byte SET(byte b, byte reg) {//----
        return (byte)(reg | b);
    }

    private byte RES(int b, byte reg) {//----
        return (byte)(reg & ~b);
    }

    private void BIT(byte b, byte reg) {//Z01-
        FlagZ = (reg & b) == 0;
        FlagN = false;
        FlagH = true;
    }

    private byte SRL(byte b) {//Z00C
        byte result = (byte)(b >> 1);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x1) != 0;
        return result;
    }

    private byte SWAP(byte b) {//Z000
        byte result = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return result;
    }

    private byte SRA(byte b) {//Z00C
        byte result = (byte)((b >> 1) | ( b & 0x80));
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x1) != 0;
        return result;
    }

    private byte SLA(byte b) {//Z00C
        byte result = (byte)(b << 1);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x80) != 0;
        return result;
    }

    private byte RR(byte b) {//Z00C
        bool prevC = FlagC;
        byte result = (byte)((b >> 1) | (prevC ? 0x80 : 0));
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x1) != 0;
        return result;
    }

    private byte RL(byte b) {//Z00C
        bool prevC = FlagC;
        byte result = (byte)((b << 1) | (prevC ? 1 : 0));
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x80) != 0;
        return result;
    }

    private byte RRC(byte b) {//Z00C
        byte result = (byte)((b >> 1) | (b << 7));
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x1) != 0;
        return result;
    }

    private byte RLC(byte b) {//Z00C
        byte result = (byte)((b << 1) | (b >> 7));
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (b & 0x80) != 0;
        return result;
    }

    private ushort DADr8(ushort w) {//00HC | warning r8 is signed!
        byte b = _mmu.Read(PC++);
        FlagZ = false;
        FlagN = false;
        SetFlagH((byte)w, b);
        SetFlagC((byte)w + b);
        return (ushort)(w + (sbyte)b);
    }

    private void JR(bool flag) {
        if (flag) {
            sbyte sb = (sbyte)_mmu.Read(PC);
            PC = (ushort)(PC + sb);
            PC += 1; //<---- //TODO WHAT?
            cycles += 12;
        } else {
            PC += 1;
            cycles += 8;
        }
    }

    private void STOP() {
        throw new NotImplementedException();
    }

    private byte INC(byte b) { //Z0H-
        int result = b + 1;
        SetFlagZ(result);
        FlagN = false;
        SetFlagH(b, 1);
        return (byte)result;
    }

    private byte DEC(byte b) { //Z1H-
        int result = b - 1;
        SetFlagZ(result);
        FlagN = true;
        SetFlagHSub(b, 1);
        return (byte)result;
    }
    private void ADD(ref int _cycles, IncsType incsType, string data1 = "", string data2 = "")
    {
        if (incsType == IncsType.R16_R16)
        {
            u16 value = 0;
            switch (data1)
            {
                case "BC": value = BC; break;
                case "DE": value = DE; break;
                case "HL": value = HL; break;
                case "SP": value = SP; break;
            }
            // set flag
            FlagN = false;
            SetFlagH(HL, value);
            FlagC =  (HL + value) >> 16 != 0;

            HL = (u16) (HL + value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }
            SetFlagZ(A + value);
            FlagN = false;
            SetFlagH(A, value);
            SetFlagC(A + value);
            A = (u8) (A + value);
            _cycles += 4;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ(A + value);
            FlagN = false;
            SetFlagH(A, value);
            SetFlagC(A + value);
            A = (u8) (A + value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u8 value = _mmu.Read(PC ++);
            SetFlagZ(A + value);
            FlagN = false;
            SetFlagH(A, value);
            SetFlagC(A + value);
            A = (u8) (A + value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R16_S8)
        {
            u8 value = _mmu.Read(PC ++);
            FlagZ = false;
            FlagN = false;
            SetFlagH((u8) SP, value);
            SetFlagC((u8) SP + value);
            SP = (u16) (SP + (sbyte) value);
            _cycles += 16;
        }
    }

    private void ADD2(byte b) { //Z0HC
        int result = A + b;
        SetFlagZ(result);
        FlagN = false;
        SetFlagH(A, b);
        SetFlagC(result);
        A = (byte)result;
    }
    private void ADC(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }

            SetFlagZ(A + value + (FlagC ? 1 : 0));
            FlagN = false;
            if (FlagC) { SetFlagHCarry(A, value); }
            else { SetFlagH(A, value); }
            A = (u8) (A + value + (FlagC ? 1 : 0));
            SetFlagC(A + value + (FlagC ? 1 : 0));
            _cycles += 4;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ(A + value + (FlagC ? 1 : 0));
            FlagN = false;
            if (FlagC) { SetFlagHCarry(A, value); }
            else { SetFlagH(A, value); }
            A = (u8) (A + value + (FlagC ? 1 : 0));
            SetFlagC(A + value + (FlagC ? 1 : 0));
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u8 value = _mmu.Read(PC ++);
            SetFlagZ(A + value + (FlagC ? 1 : 0));
            FlagN = false;
            if (FlagC) { SetFlagHCarry(A, value); }
            else { SetFlagH(A, value); }
            A = (u8) (A + value + (FlagC ? 1 : 0));
            SetFlagC(A + value + (FlagC ? 1 : 0));
            _cycles += 8;
        }
    }

    private void ADC2(byte b) { //Z0HC
        int carry = FlagC ? 1 : 0;
        int result = A + b + carry;
        SetFlagZ(result);
        FlagN = false;
        if (FlagC)
            SetFlagHCarry(A, b);
        else SetFlagH(A, b);
        SetFlagC(result);
        A = (byte)result;
    }

    private void SUB(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }
            SetFlagZ(A - value);
            FlagN = true;
            SetFlagHSub(A, value);
            SetFlagC(A - value);
            A = (u8) (A - value);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ(A - value);
            FlagN = true;
            SetFlagHSub(A, value);
            SetFlagC(A - value);
            A = (u8) (A - value);
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(PC ++);
            SetFlagZ(A - value);
            FlagN = true;
            SetFlagHSub(A, value);
            SetFlagC(A - value);
            A = (u8) (A - value);
            _cycles += 8;
        }
    }
    private void SUB2(byte b) {//Z1HC
        int result = A - b;
        SetFlagZ(result);
        FlagN = true;
        SetFlagHSub(A, b);
        SetFlagC(result);
        A = (byte)result;
    }
    private void SBC(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }

            SetFlagZ(A - value - (FlagC ? 1 : 0));
            FlagN = true;
            if (FlagC) { SetFlagHSubCarry(A, value); }
            else { SetFlagHSub(A, value); }
            A = (u8) (A - value - (FlagC ? 1 : 0));
            SetFlagC(A - value - (FlagC ? 1 : 0));
            _cycles += 4;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = _mmu.Read(HL);

            SetFlagZ(A - value - (FlagC ? 1 : 0));
            FlagN = true;
            if (FlagC) { SetFlagHSubCarry(A, value); }
            else { SetFlagHSub(A, value); }
            A = (u8) (A - value - (FlagC ? 1 : 0));
            SetFlagC(A - value - (FlagC ? 1 : 0));
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u8 value = _mmu.Read(PC ++);

            SetFlagZ(A - value - (FlagC ? 1 : 0));
            FlagN = true;
            if (FlagC) { SetFlagHSubCarry(A, value); }
            else { SetFlagHSub(A, value); }
            A = (u8) (A - value - (FlagC ? 1 : 0));
            SetFlagC(A - value - (FlagC ? 1 : 0));
            _cycles += 8;
        }
    }

    private void SBC2(byte b) {//Z1HC
        int carry = FlagC ? 1 : 0;
        int result = A - b - carry;
        SetFlagZ(result);
        FlagN = true;
        if (FlagC)
            SetFlagHSubCarry(A, b);
        else SetFlagHSub(A, b);
        SetFlagC(result);
        A = (byte)result;
    }
    private void AND(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }

            A = (u8) (A & value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = true;
            FlagC = false;

            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);

            A = (u8) (A & value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = true;
            FlagC = false;

            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(PC ++);
            A = (u8) (A & value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = true;
            FlagC = false;

            _cycles += 8;
        }
    }

    private void AND2(byte b) {//Z010
        byte result = (byte)(A & b);
        SetFlagZ(result);
        FlagN = false;
        FlagH = true;
        FlagC = false;
        A = result;
    }
    private void XOR(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }

            A = (u8) (A ^ value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = false;
            FlagC = false;

            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);

            A = (u8) (A ^ value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(PC++);

            A = (u8) (A ^ value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            _cycles += 8;
        }
    }

    private void XOR2(byte b) {//Z000
        byte result = (byte)(A ^ b);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = false;
        A = result;
    }

private void OR(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }

            A = (u8) (A | value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = false;
            FlagC = false;

            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);

            A = (u8) (A | value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(PC ++);
            A = (u8) (A | value);
            SetFlagZ(A);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            _cycles += 8;
        }
    }
    private void OR2(byte b) {//Z000
        byte result = (byte)(A | b);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = false;
        A = result;
    }

    private void CP(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = B; break;
                case "C": value = C; break;
                case "D": value = D; break;
                case "E": value = E; break;
                case "H": value = H; break;
                case "L": value = L; break;
                case "A": value = A; break;
            }
            SetFlagZ(A - value);
            FlagN = true;
            SetFlagHSub(A, value);
            SetFlagC(A - value);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ(A - value);
            FlagN = true;
            SetFlagHSub(A, value);
            SetFlagC(A - value);
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(PC ++);
            SetFlagZ(A - value);
            FlagN = true;
            SetFlagHSub(A, value);
            SetFlagC(A - value);
            _cycles += 8;
        }
    }
    private void CP2(byte b) {//Z1HC
        int result = A - b;
        SetFlagZ(result);
        FlagN = true;
        SetFlagHSub(A, b);
        SetFlagC(result);
    }

    private void DAD(ushort w) { //-0HC
        int result = HL + w;
        FlagN = false;
        SetFlagH(HL, w); //Special Flag H with word
        FlagC = result >> 16 != 0; //Special FlagC as short value involved
        HL = (ushort)result;
    }
    private void RET(ref int _cycles, bool flag)
    {
        if (flag) { PC = POP(ref _cycles); }
        _cycles += 8;
    }

    private void RETI(ref int _cycles)
    {
        PC = POP(ref _cycles);
        _cycles -= 12; // POP Cycle
        _cycles += 16;
        IME = true;
    }

    private void RETURN(bool flag) {
        if (flag) {
            PC = POP2();
            cycles += 20;
        } else {
            cycles += 8;
        }
    }

    private void CALL(bool flag) {
        if (flag) {
            PUSH((ushort)(PC + 2));
            PC = _mmu.ReadROM16(PC);
            cycles += 24;
        } else {
            PC += 2;
            cycles += 12;
        }
    }

    private void JUMP(bool flag) {
        if (flag) {
            PC = _mmu.ReadROM16(PC);
            cycles += 16;
        } else {
            PC += 2;
            cycles += 12;
        }
    }
    private void JP(ref int _cycles, bool flag, IncsType incsType = IncsType.A16)
    {
        if (incsType == IncsType.A16)
        {
            if (flag) 
            { PC = _mmu.ReadROM16(PC); _cycles += 16; }
            else { PC += 2; _cycles += 12; }
        }
        else if (incsType == IncsType.MAddr)
        {
            PC = HL;
            _cycles += 4;
        }
    }

    private void RST(byte b) {
        PUSH(PC);
        PC = b;
    }


    private void HALT(ref int _cycles)
    {
        if (!IME && ((_mmu.GetIE() & _mmu.IFRegister & 0x1F) == 0)) { HALTED = true; PC --;}
        _cycles += 4;
    }

    private void HALT2() {
        if (!IME) {
            if ((_mmu.GetIE() & _mmu.IFRegister & 0x1F) == 0) {
                HALTED = true;
                PC--;
            } else {
                HALT_BUG = true;
            }
        }
    }

    public void UpdateIME() {
        IME |= IMEEnabler;
        IMEEnabler = false;
    }

    public void ExecuteInterrupt(int b) {
        if (HALTED) {
            PC++;
            HALTED = false;
        }
        if (IME) {
            PUSH(PC);
            PC = (ushort)(0x40 + (8 * b));
            IME = false;
            
            _mmu.IFRegister = BitClear(b, _mmu.IFRegister);
        }
    }

    private u8 BitClear(int n, u8 value)
    {//b, _mmu.GetIF()
        return value &= (u8)~(1 << n);
    }
    

    private void PUSH(ushort w) {// (SP - 1) < -PC.hi; (SP - 2) < -PC.lo
        SP -= 2;
        _mmu.WriteROM16(SP, w);
    }

    private ushort POP2() {
        ushort ret = _mmu.ReadROM16(SP);
        SP += 2;
        //byte l = _mmu.Read(++SP);
        //byte h = _mmu.Read(++SP);
        //ushort ret = (ushort)(h << 8 | l);
        //Console.WriteLine("stack POP = " + ret.ToString("x4") + " SP = " + SP.ToString("x4") + " reading: " + _mmu.ReadROM16(SP).ToString("x4") + "ret = " /*+ ((ushort)(h << 8 | l)).ToString("x4")*/);


        return ret;
    }
    private u16 POP(ref int _cycles, string data1 = "")
    {
        u16 value = _mmu.ReadROM16(SP);
        SP += 2;

        switch (data1)
        {
            case "BC": BC = value; break;
            case "DE": DE = value; break;
            case "HL": HL = value; break;
            case "AF": AF = value; break;
        }
        if (data1 != "") _cycles += 12;
        return value;
    }

    private void SetFlagZ(int b) {
        FlagZ = (byte)b == 0;
    }

    private void SetFlagC(int i) {
        FlagC = (i >> 8) != 0;
    }

    private void SetFlagH(byte b1, byte b2) {
        FlagH = ((b1 & 0xF) + (b2 & 0xF)) > 0xF;
    }

    private void SetFlagH(ushort w1, ushort w2) {
        FlagH = ((w1 & 0xFFF) + (w2 & 0xFFF)) > 0xFFF;
    }

    private void SetFlagHCarry(byte b1, byte b2) {
        FlagH = ((b1 & 0xF) + (b2 & 0xF)) >= 0xF;
    }

    private void SetFlagHSub(byte b1, byte b2) {
        FlagH = (b1 & 0xF) < (b2 & 0xF);
    }

    private void SetFlagHSubCarry(byte b1, byte b2) {
        int carry = FlagC ? 1 : 0;
        FlagH = (b1 & 0xF) < ((b2 & 0xF) + carry);
    }

    private void warnUnsupportedOpcode(byte opcode) {
        Console.WriteLine((PC - 1).ToString("x4") + " Unsupported operation " + opcode.ToString("x2"));
    }

    private int dev;
    private void debug(byte opcode) {
        dev += cycles;
        if (dev >= 23440108 /*&& PC == 0x35A*/) //0x100 23440108
            Console.WriteLine("Cycle " + dev + " PC " + (PC - 1).ToString("x4") + " Stack: " + SP.ToString("x4") + " AF: " + A.ToString("x2") + "" + F.ToString("x2")
                + " BC: " + B.ToString("x2") + "" + C.ToString("x2") + " DE: " + D.ToString("x2") + "" + E.ToString("x2") + " HL: " + H.ToString("x2") + "" + L.ToString("x2")
                + " op " + opcode.ToString("x2") + " D16 " + _mmu.ReadROM16(PC).ToString("x4") + " LY: " + _mmu.LY .ToString("x2"));
    }
}