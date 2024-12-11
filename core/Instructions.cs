
public class Instructions
{
    public Registers Regs;
    // private Instr _insc;
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

    private int cycles;


    public Instructions(ref MMU mmu) {
        this._mmu = mmu;
        Regs = new Registers();
        Regs.Init();
        // _insc = new Instructions(ref Regs, ref _mmu);
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
        cycles = 0;
        switch (opcode) {
            case 0x00:                              break; //NOP        1 4     ----
            case 0x01: LD(ref cycles, IncsType.R16_D16, "BC"); return cycles; break;//BC = _mmu.ReadROM16(PC); PC += 2;      ; break; //LD BC,D16  3 12    ----
            case 0x02: LD(ref cycles, IncsType.MAddr_R8, "BC", "A"); return cycles; break;//_mmu.Write(BC, A);                ;break; //LD (BC),A  1 8     ----
            case 0x03: INC(ref cycles, IncsType.R16, "BC"); return cycles; //BC += 1;                             break; //INC BC     1 8     ----
            case 0x04: INC(ref cycles,IncsType.R8, "B"); return cycles;//B = INC(B);                          break; //INC B      1 4     Z0H-
            case 0x05: DEC(ref cycles,IncsType.R8, "B"); return cycles;B = DEC2(B);                          break; //DEC B      1 4     Z1H-
            case 0x06: LD(ref cycles, IncsType.R8_D8, "B"); return cycles; break;//B = _mmu.Read(PC); PC += 1;       ;break; //LD B,D8    2 8     ----

            case 0x07: RLCA(ref cycles); return cycles;//RLCA 1 4 000C
                // F = 0;
                // FlagC = ((A & 0x80) != 0);
                // A = (byte)((A << 1) | (A >> 7));
                // break;

            case 0x08: LD(ref cycles, IncsType.A16_R16); return cycles; break;//_mmu.WriteROM16(_mmu.ReadROM16(PC), SP); PC += 2; ;break; //LD (A16),SP 3 20   ----
            case 0x09: ADD(ref cycles,IncsType.R16_R16, "BC"); return cycles; //DAD(BC);                             break; //ADD HL,BC   1 8    -0HC
            case 0x0A: LD(ref cycles, IncsType.R8_MAddr, "A", "BC"); return cycles; break;//A = _mmu.Read(BC);  ;              break; //LD A,(BC)   1 8    ----
            case 0x0B: DEC(ref cycles, IncsType.R16, "BC"); return cycles;BC -= 1;                             break; //DEC BC      1 8    ----
            case 0x0C: INC(ref cycles,IncsType.R8, "C"); return cycles;//C = INC(C);                          break; //INC C       1 8    Z0H-
            case 0x0D: DEC(ref cycles,IncsType.R8, "C"); return cycles;C = DEC2(C);                          break; //DEC C       1 8    Z1H-
            case 0x0E: LD(ref cycles, IncsType.R8_D8, "C"); return cycles; break;//C = _mmu.Read(PC); PC += 1;  ;     break; //LD C,D8     2 8    ----

            case 0x0F: RRCA(ref cycles); return cycles;//RRCA 1 4 000C
                // F = 0;
                // FlagC = ((A & 0x1) != 0);
                // A = (byte)((A >> 1) | (A << 7));
                // break;

            case 0x11: LD(ref cycles, IncsType.R16_D16, "DE"); return cycles; break;//DE = _mmu.ReadROM16(PC); PC += 2; ;     break; //LD DE,D16   3 12   ----
            case 0x12: LD(ref cycles, IncsType.MAddr_R8, "DE", "A"); return cycles; break;//_mmu.Write(DE, A);   ;             break; //LD (DE),A   1 8    ----
            case 0x13: INC(ref cycles, IncsType.R16, "DE"); return cycles; //DE += 1;                             break; //INC DE      1 8    ----
            case 0x14: INC(ref cycles,IncsType.R8, "D"); return cycles;//D = INC(D);                          break; //INC D       1 8    Z0H-
            case 0x15: DEC(ref cycles,IncsType.R8, "D"); return cycles;D = DEC2(D);                          break; //DEC D       1 8    Z1H-
            case 0x16: LD(ref cycles, IncsType.R8_D8, "D"); return cycles; break;//D = _mmu.Read(PC); PC += 1;  ;     break; //LD D,D8     2 8    ----

            case 0x17: RLA(ref cycles); return cycles; //RLA 1 4 000C
                // bool prevC = FlagC;
                // F = 0;
                // FlagC = ((A & 0x80) != 0);
                // A = (byte)((A << 1) | (prevC ? 1 : 0));
                // break;

            case 0x18: JR(ref cycles, true); return cycles;//                      break; //JR R8       2 12   ----
            case 0x19: ADD(ref cycles, IncsType.R16_R16, "DE"); return cycles;//DAD(DE);                             break; //ADD HL,DE   1 8    -0HC
            case 0x1A: LD(ref cycles, IncsType.R8_MAddr, "A", "DE"); return cycles; break;//A = _mmu.Read(DE);     ;           break; //LD A,(DE)   1 8    ----
            case 0x1B: DEC(ref cycles, IncsType.R16, "DE"); return cycles;DE -= 1;                             break; //INC DE      1 8    ----
            case 0x1C: INC(ref cycles,IncsType.R8, "E"); return cycles;//E = INC(E);                          break; //INC E       1 8    Z0H-
            case 0x1D: DEC(ref cycles,IncsType.R8, "E"); return cycles;E = DEC2(E);                          break; //DEC E       1 8    Z1H-
            case 0x1E: LD(ref cycles, IncsType.R8_D8, "E"); return cycles; break;//E = _mmu.Read(PC); PC += 1;;       break; //LD E,D8     2 8    ----

            case 0x1F: RRA(ref cycles); return cycles;//RRA 1 4 000C
                // bool preC = FlagC;
                // F = 0;
                // FlagC = ((A & 0x1) != 0);
                // A = (byte)((A >> 1) | (preC ? 0x80 : 0));
                // break;

            case 0x20: JR(ref cycles, !FlagZ);  return cycles;                    break; //JR NZ R8    2 12/8 ---- 
            case 0x21: LD(ref cycles, IncsType.R16_D16, "HL"); return cycles; break;//HL = _mmu.ReadROM16(PC); PC += 2;      break; //LD HL,D16   3 12   ----
            case 0x22: LD(ref cycles, IncsType.MAddr_R8, "HL+", "A"); return cycles; break;//_mmu.Write(HL++, A);              break; //LD (HL+),A  1 8    ----
            case 0x23: INC(ref cycles, IncsType.R16, "HL"); return cycles; //HL += 1;                             break; //INC HL      1 8    ----
            case 0x24: INC(ref cycles,IncsType.R8, "H"); return cycles;//H = INC(H);                          break; //INC H       1 8    Z0H-
            case 0x25: DEC(ref cycles,IncsType.R8, "H"); return cycles;H = DEC2(H);                          break; //DEC H       1 8    Z1H-
            case 0x26: LD(ref cycles, IncsType.R8_D8, "H");return cycles;  break;//H = _mmu.Read(PC); PC += 1; ;     break; //LD H,D8     2 8    ----

            case 0x27: DAA(ref cycles); return cycles;//DAA    1 4 Z-0C
                // if (FlagN) { // sub
                //     if (FlagC) { A -= 0x60; }
                //     if (FlagH) { A -= 0x6; }
                // } else { // add
                //     if (FlagC || (A > 0x99)) { A += 0x60; FlagC = true; }
                //     if (FlagH || (A & 0xF) > 0x9) { A += 0x6; }
                // }
                // SetFlagZ(A);
                // FlagH = false;
                // break;

            case 0x28: JR(ref cycles, FlagZ); return cycles; //JR(FlagZ);                                 break; //JR Z R8    2 12/8  ----
            case 0x29: ADD(ref cycles, IncsType.R16_R16, "HL");return cycles; //DAD(HL);                                        break; //ADD HL,HL  1 8     -0HC
            case 0x2A: LD(ref cycles, IncsType.R8_MAddr, "A", "HL+"); return cycles; return cycles; break;//A = _mmu.Read(HL++);                         break; //LD A (HL+) 1 8     ----
            case 0x2B: DEC(ref cycles, IncsType.R16, "HL"); return cycles;HL -= 1;                                        break; //DEC HL     1 4     ----
            case 0x2C: INC(ref cycles,IncsType.R8, "L"); return cycles;//L = INC(L);                                     break; //INC L      1 4     Z0H-
            case 0x2D: DEC(ref cycles,IncsType.R8, "L"); return cycles;L = DEC2(L);                                     break; //DEC L      1 4     Z1H-
            case 0x2E: LD(ref cycles, IncsType.R8_D8, "L"); return cycles; break;//L = _mmu.Read(PC); PC += 1; ;                break; //LD L,D8    2 8     ----
            case 0x2F: CPL(ref cycles); return cycles; //A = (byte)~A; FlagN = true; FlagH = true;       break; //CPL	       1 4     -11-

            case 0x30: JR(ref cycles, !FlagC);    return cycles; //                             break; //JR NC R8   2 12/8  ----
            case 0x31: LD(ref cycles, IncsType.R16_D16, "SP"); return cycles; break;//SP = _mmu.ReadROM16(PC); PC += 2; ;               break; //LD SP,D16  3 12    ----
            case 0x32: LD(ref cycles, IncsType.MAddr_R8, "HL-", "A"); return cycles; break;//_mmu.Write(HL--, A);                         break; //LD (HL-),A 1 8     ----
            case 0x33: INC(ref cycles, IncsType.R16, "SP"); return cycles; //SP += 1;                                        break; //INC SP     1 8     ----
            case 0x34: INC(ref cycles, IncsType.MAddr, "HL"); return cycles; //_mmu.Write(HL, INC2(_mmu.Read(HL)));       break; //INC (HL)   1 12    Z0H-
            case 0x35: DEC(ref cycles, IncsType.MAddr, "HL");return cycles;// _mmu.Write(HL, DEC2(_mmu.Read(HL)));       break; //DEC (HL)   1 12    Z1H-
            case 0x36: LD(ref cycles, IncsType.MAddr_D8); return cycles; break;//_mmu.Write(HL, _mmu.Read(PC)); PC += 1;   break; //LD (HL),D8 2 12    ----
            case 0x37: SCF(ref cycles); return cycles;//FlagC = true; FlagN = false; FlagH = false;     break; //SCF	       1 4     -001

            case 0x38: JR(ref cycles, FlagC); return cycles;//                                break; //JR C R8    2 12/8  ----
            case 0x39: DAD(SP);                                        break; //ADD HL,SP  1 8     -0HC
            case 0x3A: LD(ref cycles, IncsType.R8_MAddr, "A", "HL-"); return cycles; break;//A = _mmu.Read(HL--);                         break; //LD A (HL-) 1 8     ----
            case 0x3B: DEC(ref cycles, IncsType.R16, "SP"); return cycles;SP -= 1;                                        break; //DEC SP     1 8     ----
            case 0x3C: INC(ref cycles,IncsType.R8, "A"); return cycles;//A = INC(A);                                     break; //INC A      1 4     Z0H-
            case 0x3D: DEC(ref cycles,IncsType.R8, "A"); return cycles;//A = DEC2(A);                                     break; //DEC (HL)   1 4     Z1H-
            case 0x3E: LD(ref cycles,IncsType.R8_D8, "A"); return cycles;
                // A = _mmu.Read(PC); PC += 1;   
                // break; //LD A,D8    2 8     ----
            case 0x3F: CCF(ref cycles); return cycles;//FlagC = !FlagC; FlagN = false; FlagH = false;   break; //CCF        1 4     -00C

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
            case 0x8A: ADC(ref cycles, IncsType.R8_R8, "D"); return cycles;break;//ADC2(D);                break; //ADC D	    1 4    Z0HC	
            case 0x8B: ADC(ref cycles, IncsType.R8_R8, "E"); return cycles;break;//ADC2(E);                break; //ADC E	    1 4    Z0HC	
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
            case 0xB9: CP(ref cycles, IncsType.R8, "C"); return cycles; //CP(C);                 break; //CP C     	1 4    Z1HC
            case 0xBA: CP(ref cycles, IncsType.R8, "D"); return cycles; //CP(D);                 break; //CP D     	1 4    Z1HC
            case 0xBB: CP(ref cycles, IncsType.R8, "E"); return cycles; //CP(E);                 break; //CP E     	1 4    Z1HC
            case 0xBC: CP(ref cycles, IncsType.R8, "H"); return cycles; //CP(H);                 break; //CP H     	1 4    Z1HC
            case 0xBD: CP(ref cycles, IncsType.R8, "L"); return cycles; //CP(L);                 break; //CP L     	1 4    Z1HC
            case 0xBE: CP(ref cycles, IncsType.MAddr); return cycles;// CP(_mmu.Read(HL));  break; //CP M     	1 8    Z1HC
            case 0xBF: CP(ref cycles, IncsType.R8, "A"); return cycles; //CP(A);                 break; //CP A     	1 4    Z1HC

            case 0xC0: RET(ref cycles, !FlagZ); return cycles; //RETURN(!FlagZ);             break; //RET NZ	     1 20/8  ----
            case 0xC1: POP(ref cycles, "BC"); return cycles; //BC = POP();                   break; //POP BC      1 12    ----
            case 0xC2: JP(ref cycles, !FlagZ); return cycles; //JUMP(!FlagZ);               break; //JP NZ,A16   3 16/12 ----
            case 0xC3: JP(ref cycles, true); return cycles; //JUMP(true);                 break; //JP A16      3 16    ----
            case 0xC4: CALL(ref cycles, !FlagZ); return cycles;//CALL(!FlagZ);               break; //CALL NZ A16 3 24/12 ----
            case 0xC5: PUSH(ref cycles, "BC");return cycles;//PUSH(BC);                   break; //PUSH BC     1 16    ----
            case 0xC6: ADD(ref cycles, IncsType.R8_D8); return cycles; break;//ADD(_mmu.Read(PC)); PC += 1;  break; //ADD A,D8    2 8     Z0HC
            case 0xC7: RST(ref cycles,0x0); return cycles; //RST(0x0);                   break; //RST 0       1 16    ----

            case 0xC8: RET(ref cycles, FlagZ); return cycles;//RETURN(FlagZ);              break; //RET Z       1 20/8  ----
            case 0xC9: RET(ref cycles, true); return cycles;// RETURN(true);               break; //RET         1 16    ----
            case 0xCA: JP(ref cycles, FlagZ); return cycles; //               break; //JP Z,A16    3 16/12 ----
            case 0xCB: return PREFIX_CB(_mmu.Read(PC++));      break; //PREFIX CB OPCODE TABLE
            case 0xCC: CALL(ref cycles, FlagZ); return cycles;//CALL(FlagZ);                break; //CALL Z,A16  3 24/12 ----
            case 0xCD: CALL(ref cycles, true); return cycles;//CALL(true);                 break; //CALL A16    3 24    ----
            case 0xCE: ADC(ref cycles, IncsType.R8_D8); return cycles; break;//ADC(_mmu.Read(PC)); PC += 1;  break; //ADC A,D8    2 8     ----
            case 0xCF: RST(ref cycles,0x8); return cycles; //RST(0x8);                   break; //RST 1 08    1 16    ----

            case 0xD0: RET(ref cycles, !FlagC); return cycles;//RETURN(!FlagC);             break; //RET NC      1 20/8  ----
            case 0xD1: POP(ref cycles, "DE"); return cycles; //DE = POP();                   break; //POP DE      1 12    ----
            case 0xD2: JP(ref cycles, !FlagC); return cycles; //JUMP(!FlagC);               break; //JP NC,A16   3 16/12 ----
            //case 0xD3:                                break; //Illegal Opcode
            case 0xD4: CALL(ref cycles, !FlagC); return cycles;//CALL(!FlagC);               break; //CALL NC,A16 3 24/12 ----
            case 0xD5: PUSH(ref cycles, "DE");return cycles;//PUSH(DE);                   break; //PUSH DE     1 16    ----
            case 0xD6: SUB(ref cycles, IncsType.D8); return cycles;break;//SUB(_mmu.Read(PC)); PC += 1;  break; //SUB D8      2 8     ----
            case 0xD7: RST(ref cycles,0x10); return cycles; //RST(0x10);                  break; //RST 2 10    1 16    ----

            case 0xD8: RET(ref cycles, FlagC); return cycles;//RETURN(FlagC);              break; //RET C       1 20/8  ----
            case 0xD9: RETI(ref cycles); return cycles; //RETURN(true); IME = true;   break; //RETI        1 16    ----
            case 0xDA: JP(ref cycles, FlagC); return cycles; //JUMP(FlagC);                break; //JP C,A16    3 16/12 ----
            //case 0xDB:                                break; //Illegal Opcode
            case 0xDC: CALL(ref cycles, FlagC); return cycles;//CALL(FlagC);                break; //Call C,A16  3 24/12 ----
            //case 0xDD:                                break; //Illegal Opcode
            case 0xDE: SBC(ref cycles, IncsType.R8_D8); return cycles;//SBC(_mmu.Read(PC)); PC += 1;  break; //SBC A,A8    2 8     Z1HC
            case 0xDF: RST(ref cycles,0x18); return cycles; //RST(0x18);                  break; //RST 3 18    1 16    ----

            case 0xE0:  LD(ref cycles,IncsType.A8_R8); return cycles;//LDH (A8),A 2 12 ----
                // ushort value1 = (ushort)(0xFF00 + _mmu.Read(PC));
                // byte value2 = A;
                // _mmu.Write((ushort)(0xFF00 + _mmu.Read(PC)), A); 
                // PC += 1;  
                // break;
            case 0xE1: POP(ref cycles, "HL"); return cycles; //HL = POP();                   break; //POP HL      1 12    ----
            case 0xE2: LD(ref cycles,IncsType.MAddr8_R8); return cycles;//_mmu.Write((ushort)(0xFF00 + C), A);   break; //LD (C),A   1 8  ----
            //case 0xE3:                                break; //Illegal Opcode
            //case 0xE4:                                break; //Illegal Opcode
            case 0xE5: PUSH(ref cycles, "HL");return cycles;//PUSH(HL);                   break; //PUSH HL     1 16    ----
            case 0xE6: AND(ref cycles, IncsType.D8); return cycles;//AND(_mmu.Read(PC)); PC += 1;  break; //AND D8      2 8     Z010
            case 0xE7: RST(ref cycles,0x20); return cycles; //RST(0x20);                  break; //RST 4 20    1 16    ----

            case 0xE8: ADD(ref cycles,IncsType.R16_S8);return cycles;   //SP = DADr8(SP);             break; //ADD SP,R8   2 16    00HC
            case 0xE9: JP(ref cycles,true, IncsType.MAddr); return cycles; //PC = HL;                         break; //JP (HL)     1 4     ----
            case 0xEA: LD(ref cycles,IncsType.A16_R8);return cycles; //_mmu.Write(_mmu.ReadROM16(PC), A); PC += 2;                     break; //LD (A16),A 3 16 ----
            //case 0xEB:                                break; //Illegal Opcode
            //case 0xEC:                                break; //Illegal Opcode
            //case 0xED:                                break; //Illegal Opcode
            case 0xEE: XOR(ref cycles, IncsType.D8); return cycles; //XOR2(_mmu.Read(PC)); PC += 1;  break; //XOR D8      2 8     Z000
            case 0xEF: RST(ref cycles,0x28); return cycles; //RST(0x28);                  break; //RST 5 28    1 16    ----

            case 0xF0: LD(ref cycles,IncsType.R8_A8);return cycles; //A = _mmu.Read((ushort)(0xFF00 + _mmu.Read(PC))); PC += 1;  break; //LDH A,(A8)  2 12    ----
            case 0xF1: POP(ref cycles, "AF"); return cycles; //AF = POP();                   break; //POP AF      1 12    ZNHC
            case 0xF2: LD(ref cycles, IncsType.R8_MAddr8);return cycles; //A = _mmu.Read((ushort)(0xFF00 + C));  break; //LD A,(C)    1 8     ----
            case 0xF3: DI(ref cycles); return cycles;//IME = false;                     break; //DI          1 4     ----
            //case 0xF4:                                break; //Illegal Opcode
            case 0xF5: PUSH(ref cycles, "AF");return cycles;//PUSH(AF);                   break; //PUSH AF     1 16    ----
            case 0xF6: OR(ref cycles, IncsType.D8); return cycles; //OR(_mmu.Read(PC)); PC += 1;   break; //OR D8       2 8     Z000
            case 0xF7: RST(ref cycles,0x30); return cycles; //RST(0x30);                  break; //RST 6 30    1 16    ----

            case 0xF8: LD(ref cycles, IncsType.R16_R16S8); return cycles; //HL = DADr8(SP);             break; //LD HL,SP+R8 2 12    00HC
            case 0xF9: LD(ref cycles, IncsType.R16_R16);return cycles; //SP = HL;                         break; //LD SP,HL    1 8     ----
            case 0xFA: LD(ref cycles, IncsType.R8_A16);return cycles; //A = _mmu.Read(_mmu.ReadROM16(PC)); PC += 2;   break; //LD A,(A16)  3 16    ----
            case 0xFB: EI(ref cycles); return cycles;//IMEEnabler = true;               break; //IE          1 4     ----
            //case 0xFC:                                break; //Illegal Opcode
            //case 0xFD:                                break; //Illegal Opcode
            case 0xFE: CP(ref cycles, IncsType.D8); return cycles; //CP(_mmu.Read(PC)); PC += 1;   break; //CP D8       2 8     Z1HC
            case 0xFF: RST(ref cycles,0x38); return cycles; //RST(0x38);                  break; //RST 7 38    1 16    ----

            default: warnUnsupportedOpcode(opcode);     break;
        }
        cycles += _incCycles[opcode];
        return cycles;
    }
    private void CCF(ref int _cycles)
    {
        FlagC = !FlagC;
        FlagN = false;
        FlagH = false;
        _cycles += 4;
    }
    private void SCF(ref int _cycles)
    {
        FlagC = true;
        FlagN = false;
        FlagH = false;
        _cycles += 4;
    }
    private void CPL(ref int _cycles)
    {
        A = (u8) (~A);
        FlagN = true;
        FlagH = true;
        _cycles += 4;
    }
    private void RRA(ref int _cycles)
    {
        bool oldC = FlagC;
        F = 0;
        FlagC = ((A & 0x01) != 0);
        A = (u8) ((A >> 1) | (oldC ? 0x80 : 0));
        _cycles += 4;
    }
    private void DAA(ref int _cycles)
    {
        if (FlagN)
        {
            if (FlagC) A -= 0x60;
            if (FlagH) A -= 0x06;
        }
        else
        {
            if (FlagC || (A > 0x99)) { A += 0x60; FlagC = true; }
            if (FlagH || ((A) & 0x0F) > 0x09) A += 0x06;
        }
        SetFlagZ(A);
        FlagH = false;
        _cycles += 4;
    }
    private void RLA(ref int _cycles)
    {
        bool oldC = FlagC;
        F = 0;
        FlagC = ((A & 0x80) != 0);
        A = (u8) ((A << 1) | (oldC ? 1 : 0));
        _cycles += 4;
    }
    private void RRCA(ref int _cycles)
    {
        F = 0;
        FlagC = ((A & 0x01) != 0);
        A = (u8) ((A >> 1) | (A << 7));

        _cycles += 4;
    }
    private void RLCA(ref int _cycles)
    {
        F = 0;
        FlagC =  ((A & 0x80) != 0);
        A = (u8) ((A << 1) | (A >> 7));
        _cycles += 4;
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

    private int PREFIX_CB(byte opcode) {
        switch (opcode) {
            case 0x00: RLC(ref cycles, IncsType.R8, "B"); return cycles; //RLC(B);                                  break; //RLC B    2   8   Z00C
            case 0x01: RLC(ref cycles, IncsType.R8, "C"); return cycles; //RLC(C);                                  break; //RLC C    2   8   Z00C
            case 0x02: RLC(ref cycles, IncsType.R8, "D"); return cycles; //RLC(D);                                  break; //RLC D    2   8   Z00C
            case 0x03: RLC(ref cycles, IncsType.R8, "E"); return cycles; //RLC(E);                                  break; //RLC E    2   8   Z00C
            case 0x04: RLC(ref cycles, IncsType.R8, "H"); return cycles; //RLC(H);                                  break; //RLC H    2   8   Z00C
            case 0x05: RLC(ref cycles, IncsType.R8, "L"); return cycles; //RLC(L);                                  break; //RLC L    2   8   Z00C
            case 0x06: RLC(ref cycles, IncsType.MAddr); return cycles; //_mmu.Write(HL, RLC(_mmu.Read(HL)));    break; //RLC (HL) 2   8   Z00C
            case 0x07: RLC(ref cycles, IncsType.R8, "A"); return cycles; //RLC(A);                                  break; //RLC B    2   8   Z00C
                                                                    
            case 0x08: RRC(ref cycles,IncsType.R8, "B"); return cycles; //B = RRC(B);                                  break; //RRC B    2   8   Z00C
            case 0x09: RRC(ref cycles,IncsType.R8, "C"); return cycles; //C = RRC(C);                                  break; //RRC C    2   8   Z00C
            case 0x0A: RRC(ref cycles,IncsType.R8, "D"); return cycles; //D = RRC(D);                                  break; //RRC D    2   8   Z00C
            case 0x0B: RRC(ref cycles,IncsType.R8, "E"); return cycles; //E = RRC(E);                                  break; //RRC E    2   8   Z00C
            case 0x0C: RRC(ref cycles,IncsType.R8, "H"); return cycles; //H = RRC(H);                                  break; //RRC H    2   8   Z00C
            case 0x0D: RRC(ref cycles,IncsType.R8, "L"); return cycles; //L = RRC(L);                                  break; //RRC L    2   8   Z00C
            case 0x0E: RRC(ref cycles,IncsType.MAddr); return cycles; //_mmu.Write(HL, RRC(_mmu.Read(HL)));    break; //RRC (HL) 2   8   Z00C
            case 0x0F: RRC(ref cycles,IncsType.R8, "A"); return cycles; //A = RRC(A);                                  break; //RRC B    2   8   Z00C
                                                                        
            case 0x10: RL(ref cycles, IncsType.R8, "B"); return cycles; //B = RL(B);                                   break; //RL B     2   8   Z00C
            case 0x11: RL(ref cycles, IncsType.R8, "C"); return cycles; //C = RL(C);                                   break; //RL C     2   8   Z00C
            case 0x12: RL(ref cycles, IncsType.R8, "D"); return cycles; //D = RL(D);                                   break; //RL D     2   8   Z00C
            case 0x13: RL(ref cycles, IncsType.R8, "E"); return cycles; //E = RL(E);                                   break; //RL E     2   8   Z00C
            case 0x14: RL(ref cycles, IncsType.R8, "H"); return cycles; //H = RL(H);                                   break; //RL H     2   8   Z00C
            case 0x15: RL(ref cycles, IncsType.R8, "L"); return cycles; //L = RL(L);                                   break; //RL L     2   8   Z00C
            case 0x16: RL(ref cycles, IncsType.MAddr); return cycles; // _mmu.Write(HL, RL(_mmu.Read(HL)));     break; //RL (HL)  2   8   Z00C
            case 0x17: RL(ref cycles, IncsType.R8, "A"); return cycles; //A = RL(A);                                   break; //RL B     2   8   Z00C
                                                                                            
            case 0x18: RR(ref cycles, IncsType.R8, "B"); return cycles; //B = RR(B);                                   break; //RR B     2   8   Z00C
            case 0x19: RR(ref cycles, IncsType.R8, "C"); return cycles; //C = RR(C);                                   break; //RR C     2   8   Z00C
            case 0x1A: RR(ref cycles, IncsType.R8, "D"); return cycles; //D = RR(D);                                   break; //RR D     2   8   Z00C
            case 0x1B: RR(ref cycles, IncsType.R8, "E"); return cycles; //E = RR(E);                                   break; //RR E     2   8   Z00C
            case 0x1C: RR(ref cycles, IncsType.R8, "H"); return cycles; //H = RR(H);                                   break; //RR H     2   8   Z00C
            case 0x1D: RR(ref cycles, IncsType.R8, "L"); return cycles; //L = RR(L);                                   break; //RR L     2   8   Z00C
            case 0x1E: RR(ref cycles, IncsType.MAddr); return cycles; //_mmu.Write(HL, RR(_mmu.Read(HL)));     break; //RR (HL)  2   8   Z00C
            case 0x1F: RR(ref cycles, IncsType.R8, "A"); return cycles; //A = RR(A);                                   break; //RR B     2   8   Z00C
                                                                        
            case 0x20: SLA(ref cycles, IncsType.R8, "B"); return cycles; //B = SLA(B);                                  break; //SLA B    2   8   Z00C
            case 0x21: SLA(ref cycles, IncsType.R8, "C"); return cycles; //C = SLA(C);                                  break; //SLA C    2   8   Z00C
            case 0x22: SLA(ref cycles, IncsType.R8, "D"); return cycles; //D = SLA(D);                                  break; //SLA D    2   8   Z00C
            case 0x23: SLA(ref cycles, IncsType.R8, "E"); return cycles; //E = SLA(E);                                  break; //SLA E    2   8   Z00C
            case 0x24: SLA(ref cycles, IncsType.R8, "H"); return cycles; //H = SLA(H);                                  break; //SLA H    2   8   Z00C
            case 0x25: SLA(ref cycles, IncsType.R8, "L"); return cycles; //L = SLA(L);                                  break; //SLA L    2   8   Z00C
            case 0x26: SLA(ref cycles, IncsType.MAddr); return cycles; //_mmu.Write(HL, SLA(_mmu.Read(HL)));    break; //SLA (HL) 2   8   Z00C
            case 0x27: SLA(ref cycles, IncsType.R8, "A"); return cycles; //A = SLA(A);                                  break; //SLA B    2   8   Z00C
                                                                        
            case 0x28: SRA(ref cycles, IncsType.R8, "B"); return cycles; //B = SRA(B);                                  break; //SRA B    2   8   Z00C
            case 0x29: SRA(ref cycles, IncsType.R8, "C"); return cycles; //C = SRA(C);                                  break; //SRA C    2   8   Z00C
            case 0x2A: SRA(ref cycles, IncsType.R8, "D"); return cycles; //D = SRA(D);                                  break; //SRA D    2   8   Z00C
            case 0x2B: SRA(ref cycles, IncsType.R8, "E"); return cycles; //E = SRA(E);                                  break; //SRA E    2   8   Z00C
            case 0x2C: SRA(ref cycles, IncsType.R8, "H"); return cycles; //H = SRA(H);                                  break; //SRA H    2   8   Z00C
            case 0x2D: SRA(ref cycles, IncsType.R8, "L"); return cycles; //L = SRA(L);                                  break; //SRA L    2   8   Z00C
            case 0x2E: SRA(ref cycles, IncsType.MAddr); return cycles; //_mmu.Write(HL, SRA(_mmu.Read(HL)));    break; //SRA (HL) 2   8   Z00C
            case 0x2F: SRA(ref cycles, IncsType.R8, "A"); return cycles; //A = SRA(A);                                  break; //SRA B    2   8   Z00C
                                                                        
            case 0x30: SWAP(ref cycles, IncsType.R8, "B"); return cycles; //B = SWAP(B);                                 break; //SWAP B    2   8   Z00C
            case 0x31: SWAP(ref cycles, IncsType.R8, "C"); return cycles; //C = SWAP(C);                                 break; //SWAP C    2   8   Z00C
            case 0x32: SWAP(ref cycles, IncsType.R8, "D"); return cycles; //D = SWAP(D);                                 break; //SWAP D    2   8   Z00C
            case 0x33: SWAP(ref cycles, IncsType.R8, "E"); return cycles; //E = SWAP(E);                                 break; //SWAP E    2   8   Z00C
            case 0x34: SWAP(ref cycles, IncsType.R8, "H"); return cycles; //H = SWAP(H);                                 break; //SWAP H    2   8   Z00C
            case 0x35: SWAP(ref cycles, IncsType.R8, "L"); return cycles; //L = SWAP(L);                                 break; //SWAP L    2   8   Z00C
            case 0x36: SWAP(ref cycles, IncsType.MAddr); return cycles; //_mmu.Write(HL, SWAP(_mmu.Read(HL)));   break; //SWAP (HL) 2   8   Z00C
            case 0x37: SWAP(ref cycles, IncsType.R8, "A"); return cycles; //A = SWAP(A);                                 break; //SWAP B    2   8   Z00C
                                                                        
            case 0x38: SRL(ref cycles, IncsType.R8, "B"); return cycles; //B = SRL(B);                                  break; //SRL B    2   8   Z000
            case 0x39: SRL(ref cycles, IncsType.R8, "C"); return cycles; //C = SRL(C);                                  break; //SRL C    2   8   Z000
            case 0x3A: SRL(ref cycles, IncsType.R8, "D"); return cycles; //D = SRL(D);                                  break; //SRL D    2   8   Z000
            case 0x3B: SRL(ref cycles, IncsType.R8, "E"); return cycles; //E = SRL(E);                                  break; //SRL E    2   8   Z000
            case 0x3C: SRL(ref cycles, IncsType.R8, "H"); return cycles; //H = SRL(H);                                  break; //SRL H    2   8   Z000
            case 0x3D: SRL(ref cycles, IncsType.R8, "L"); return cycles; //L = SRL(L);                                  break; //SRL L    2   8   Z000
            case 0x3E: SRL(ref cycles, IncsType.MAddr); return cycles; //_mmu.Write(HL, SRL(_mmu.Read(HL)));    break; //SRL (HL) 2   8   Z000
            case 0x3F: SRL(ref cycles, IncsType.R8, "A"); return cycles; //A = SRL(A);                                  break; //SRL B    2   8   Z000

            case 0x40: BIT(ref cycles, IncsType.R8, 0x01, "B"); return cycles; //BIT(0x1, B);                                 break; //BIT B    2   8   Z01-
            case 0x41: BIT(ref cycles, IncsType.R8, 0x01, "C"); return cycles; //BIT(0x1, C);                                 break; //BIT C    2   8   Z01-
            case 0x42: BIT(ref cycles, IncsType.R8, 0x01, "D"); return cycles; //BIT(0x1, D);                                 break; //BIT D    2   8   Z01-
            case 0x43: BIT(ref cycles, IncsType.R8, 0x01, "E"); return cycles; //BIT(0x1, E);                                 break; //BIT E    2   8   Z01-
            case 0x44: BIT(ref cycles, IncsType.R8, 0x01, "H"); return cycles; //BIT(0x1, H);                                 break; //BIT H    2   8   Z01-
            case 0x45: BIT(ref cycles, IncsType.R8, 0x01, "L"); return cycles; //BIT(0x1, L);                                 break; //BIT L    2   8   Z01-
            case 0x46: BIT(ref cycles, IncsType.MAddr, 0x01); return cycles; //BIT(0x1, _mmu.Read(HL));                  break; //BIT (HL) 2   8   Z01-
            case 0x47: BIT(ref cycles, IncsType.R8, 0x01, "A"); return cycles; //BIT(0x1, A);                                 break; //BIT B    2   8   Z01-

            case 0x48: BIT(ref cycles, IncsType.R8, 0x02, "B"); return cycles; //BIT(0x2, B);                                break; //BIT B    2   8   Z01-
            case 0x49: BIT(ref cycles, IncsType.R8, 0x02, "C"); return cycles; //BIT(0x2, C);                                break; //BIT C    2   8   Z01-
            case 0x4A: BIT(ref cycles, IncsType.R8, 0x02, "D"); return cycles; //BIT(0x2, D);                                break; //BIT D    2   8   Z01-
            case 0x4B: BIT(ref cycles, IncsType.R8, 0x02, "E"); return cycles; //BIT(0x2, E);                                break; //BIT E    2   8   Z01-
            case 0x4C: BIT(ref cycles, IncsType.R8, 0x02, "H"); return cycles; //BIT(0x2, H);                                break; //BIT H    2   8   Z01-
            case 0x4D: BIT(ref cycles, IncsType.R8, 0x02, "L"); return cycles; //BIT(0x2, L);                                break; //BIT L    2   8   Z01-
            case 0x4E: BIT(ref cycles, IncsType.MAddr, 0x02); return cycles; //BIT(0x2, _mmu.Read(HL));                 break; //BIT (HL) 2   8   Z01-
            case 0x4F: BIT(ref cycles, IncsType.R8, 0x02, "A"); return cycles; //BIT(0x2, A);                                break; //BIT B    2   8   Z01-
                                                                    
            case 0x50: BIT(ref cycles, IncsType.R8, 0x04, "B"); return cycles; //break;
            case 0x51: BIT(ref cycles, IncsType.R8, 0x04, "C"); return cycles; //break;
            case 0x52: BIT(ref cycles, IncsType.R8, 0x04, "D"); return cycles; //break;
            case 0x53: BIT(ref cycles, IncsType.R8, 0x04, "E"); return cycles; //break;
            case 0x54: BIT(ref cycles, IncsType.R8, 0x04, "H"); return cycles; //break;
            case 0x55: BIT(ref cycles, IncsType.R8, 0x04, "L"); return cycles; //break;
            case 0x56: BIT(ref cycles, IncsType.MAddr, 0x04); return cycles; //break;
            case 0x57: BIT(ref cycles, IncsType.R8, 0x04, "A"); return cycles; //break;
            case 0x58: BIT(ref cycles, IncsType.R8, 0x08, "B"); return cycles; //break;
            case 0x59: BIT(ref cycles, IncsType.R8, 0x08, "C"); return cycles; //break;
            case 0x5A: BIT(ref cycles, IncsType.R8, 0x08, "D"); return cycles; //break;
            case 0x5B: BIT(ref cycles, IncsType.R8, 0x08, "E"); return cycles; //break;
            case 0x5C: BIT(ref cycles, IncsType.R8, 0x08, "H"); return cycles; //break;
            case 0x5D: BIT(ref cycles, IncsType.R8, 0x08, "L"); return cycles; //break;
            case 0x5E: BIT(ref cycles, IncsType.MAddr, 0x08); return cycles; //break;
            case 0x5F: BIT(ref cycles, IncsType.R8, 0x08, "A"); return cycles; //break;                             break; //BIT B    2   8   Z01-

            case 0x60: BIT(ref cycles, IncsType.R8, 0x10, "B"); return cycles; //break;
            case 0x61: BIT(ref cycles, IncsType.R8, 0x10, "C"); return cycles; //break;
            case 0x62: BIT(ref cycles, IncsType.R8, 0x10, "D"); return cycles; //break;
            case 0x63: BIT(ref cycles, IncsType.R8, 0x10, "E"); return cycles; //break;
            case 0x64: BIT(ref cycles, IncsType.R8, 0x10, "H"); return cycles; //break;
            case 0x65: BIT(ref cycles, IncsType.R8, 0x10, "L"); return cycles; //break;
            case 0x66: BIT(ref cycles, IncsType.MAddr, 0x10); return cycles; //break;
            case 0x67: BIT(ref cycles, IncsType.R8, 0x10, "A"); return cycles; //break;
            case 0x68: BIT(ref cycles, IncsType.R8, 0x20, "B"); return cycles; //break;
            case 0x69: BIT(ref cycles, IncsType.R8, 0x20, "C"); return cycles; //break;
            case 0x6A: BIT(ref cycles, IncsType.R8, 0x20, "D"); return cycles; //break;
            case 0x6B: BIT(ref cycles, IncsType.R8, 0x20, "E"); return cycles; //break;
            case 0x6C: BIT(ref cycles, IncsType.R8, 0x20, "H"); return cycles; //break;
            case 0x6D: BIT(ref cycles, IncsType.R8, 0x20, "L"); return cycles; //break;
            case 0x6E: BIT(ref cycles, IncsType.MAddr, 0x20); return cycles; //break;
            case 0x6F: BIT(ref cycles, IncsType.R8, 0x20, "A"); return cycles; //break;                           break; //BIT B    2   8   Z01-

            case 0x70: BIT(ref cycles, IncsType.R8, 0x40, "B"); return cycles; //break;
            case 0x71: BIT(ref cycles, IncsType.R8, 0x40, "C"); return cycles; //break;
            case 0x72: BIT(ref cycles, IncsType.R8, 0x40, "D"); return cycles; //break;
            case 0x73: BIT(ref cycles, IncsType.R8, 0x40, "E"); return cycles; //break;
            case 0x74: BIT(ref cycles, IncsType.R8, 0x40, "H"); return cycles; //break;
            case 0x75: BIT(ref cycles, IncsType.R8, 0x40, "L"); return cycles; //break;
            case 0x76: BIT(ref cycles, IncsType.MAddr, 0x40); return cycles; //break;
            case 0x77: BIT(ref cycles, IncsType.R8, 0x40, "A"); return cycles; //break;
            case 0x78: BIT(ref cycles, IncsType.R8, 0x80, "B"); return cycles; //break;
            case 0x79: BIT(ref cycles, IncsType.R8, 0x80, "C"); return cycles; //break;
            case 0x7A: BIT(ref cycles, IncsType.R8, 0x80, "D"); return cycles; //break;
            case 0x7B: BIT(ref cycles, IncsType.R8, 0x80, "E"); return cycles; //break;
            case 0x7C: BIT(ref cycles, IncsType.R8, 0x80, "H"); return cycles; //break;
            case 0x7D: BIT(ref cycles, IncsType.R8, 0x80, "L"); return cycles; //break;
            case 0x7E: BIT(ref cycles, IncsType.MAddr, 0x80); return cycles; //break;
            case 0x7F: BIT(ref cycles, IncsType.R8, 0x80, "A"); return cycles; //break;

            case 0x80: RES(ref cycles, IncsType.R8, 0x01, "B"); return cycles; //break;
            case 0x81: RES(ref cycles, IncsType.R8, 0x01, "C"); return cycles; //break;
            case 0x82: RES(ref cycles, IncsType.R8, 0x01, "D"); return cycles; //break;
            case 0x83: RES(ref cycles, IncsType.R8, 0x01, "E"); return cycles; //break;
            case 0x84: RES(ref cycles, IncsType.R8, 0x01, "H"); return cycles; //break;
            case 0x85: RES(ref cycles, IncsType.R8, 0x01, "L"); return cycles; //break;
            case 0x86: RES(ref cycles, IncsType.MAddr, 0x01); return cycles; //break;
            case 0x87: RES(ref cycles, IncsType.R8, 0x01, "A"); return cycles; //break;
            case 0x88: RES(ref cycles, IncsType.R8, 0x02, "B"); return cycles; //break;
            case 0x89: RES(ref cycles, IncsType.R8, 0x02, "C"); return cycles; //break;
            case 0x8A: RES(ref cycles, IncsType.R8, 0x02, "D"); return cycles; //break;
            case 0x8B: RES(ref cycles, IncsType.R8, 0x02, "E"); return cycles; //break;
            case 0x8C: RES(ref cycles, IncsType.R8, 0x02, "H"); return cycles; //break;
            case 0x8D: RES(ref cycles, IncsType.R8, 0x02, "L"); return cycles; //break;
            case 0x8E: RES(ref cycles, IncsType.MAddr, 0x02); return cycles; //break;
            case 0x8F: RES(ref cycles, IncsType.R8, 0x02, "A"); return cycles; //break;

            case 0x90: RES(ref cycles, IncsType.R8, 0x04, "B"); return cycles; //break;
            case 0x91: RES(ref cycles, IncsType.R8, 0x04, "C"); return cycles; //break;
            case 0x92: RES(ref cycles, IncsType.R8, 0x04, "D"); return cycles; //break;
            case 0x93: RES(ref cycles, IncsType.R8, 0x04, "E"); return cycles; //break;
            case 0x94: RES(ref cycles, IncsType.R8, 0x04, "H"); return cycles; //break;
            case 0x95: RES(ref cycles, IncsType.R8, 0x04, "L"); return cycles; //break;
            case 0x96: RES(ref cycles, IncsType.MAddr, 0x04); return cycles; //break;
            case 0x97: RES(ref cycles, IncsType.R8, 0x04, "A"); return cycles; //break;
            case 0x98: RES(ref cycles, IncsType.R8, 0x08, "B"); return cycles; //break;
            case 0x99: RES(ref cycles, IncsType.R8, 0x08, "C"); return cycles; //break;
            case 0x9A: RES(ref cycles, IncsType.R8, 0x08, "D"); return cycles; //break;
            case 0x9B: RES(ref cycles, IncsType.R8, 0x08, "E"); return cycles; //break;
            case 0x9C: RES(ref cycles, IncsType.R8, 0x08, "H"); return cycles; //break;
            case 0x9D: RES(ref cycles, IncsType.R8, 0x08, "L"); return cycles; //break;
            case 0x9E: RES(ref cycles, IncsType.MAddr, 0x08); return cycles; //break;
            case 0x9F: RES(ref cycles, IncsType.R8, 0x08, "A"); return cycles; //break;

            case 0xA0: RES(ref cycles, IncsType.R8, 0x10, "B"); return cycles; //break;
            case 0xA1: RES(ref cycles, IncsType.R8, 0x10, "C"); return cycles; //break;
            case 0xA2: RES(ref cycles, IncsType.R8, 0x10, "D"); return cycles; //break;
            case 0xA3: RES(ref cycles, IncsType.R8, 0x10, "E"); return cycles; //break;
            case 0xA4: RES(ref cycles, IncsType.R8, 0x10, "H"); return cycles; //break;
            case 0xA5: RES(ref cycles, IncsType.R8, 0x10, "L"); return cycles; //break;
            case 0xA6: RES(ref cycles, IncsType.MAddr, 0x10); return cycles; //break;
            case 0xA7: RES(ref cycles, IncsType.R8, 0x10, "A"); return cycles; //break;
            case 0xA8: RES(ref cycles, IncsType.R8, 0x20, "B"); return cycles; //break;
            case 0xA9: RES(ref cycles, IncsType.R8, 0x20, "C"); return cycles; //break;
            case 0xAA: RES(ref cycles, IncsType.R8, 0x20, "D"); return cycles; //break;
            case 0xAB: RES(ref cycles, IncsType.R8, 0x20, "E"); return cycles; //break;
            case 0xAC: RES(ref cycles, IncsType.R8, 0x20, "H"); return cycles; //break;
            case 0xAD: RES(ref cycles, IncsType.R8, 0x20, "L"); return cycles; //break;
            case 0xAE: RES(ref cycles, IncsType.MAddr, 0x20); return cycles; //break;
            case 0xAF: RES(ref cycles, IncsType.R8, 0x20, "A"); return cycles; //break;

            case 0xB0: RES(ref cycles, IncsType.R8, 0x40, "B"); return cycles; //break;
            case 0xB1: RES(ref cycles, IncsType.R8, 0x40, "C"); return cycles; //break;
            case 0xB2: RES(ref cycles, IncsType.R8, 0x40, "D"); return cycles; //break;
            case 0xB3: RES(ref cycles, IncsType.R8, 0x40, "E"); return cycles; //break;
            case 0xB4: RES(ref cycles, IncsType.R8, 0x40, "H"); return cycles; //break;
            case 0xB5: RES(ref cycles, IncsType.R8, 0x40, "L"); return cycles; //break;
            case 0xB6: RES(ref cycles, IncsType.MAddr, 0x40); return cycles; //break;
            case 0xB7: RES(ref cycles, IncsType.R8, 0x40, "A"); return cycles; //break;
            case 0xB8: RES(ref cycles, IncsType.R8, 0x80, "B"); return cycles; //break;
            case 0xB9: RES(ref cycles, IncsType.R8, 0x80, "C"); return cycles; //break;
            case 0xBA: RES(ref cycles, IncsType.R8, 0x80, "D"); return cycles; //break;
            case 0xBB: RES(ref cycles, IncsType.R8, 0x80, "E"); return cycles; //break;
            case 0xBC: RES(ref cycles, IncsType.R8, 0x80, "H"); return cycles; //break;
            case 0xBD: RES(ref cycles, IncsType.R8, 0x80, "L"); return cycles; //break;
            case 0xBE: RES(ref cycles, IncsType.MAddr, 0x80); return cycles; //break;
            case 0xBF: RES(ref cycles, IncsType.R8, 0x80, "A"); return cycles; //break;

            // 0xC0 ~ 0xCF
            case 0xC0: SET(ref cycles, IncsType.R8, 0x01, "B"); return cycles; //break;
            case 0xC1: SET(ref cycles, IncsType.R8, 0x01, "C"); return cycles; //break;
            case 0xC2: SET(ref cycles, IncsType.R8, 0x01, "D"); return cycles; //break;
            case 0xC3: SET(ref cycles, IncsType.R8, 0x01, "E"); return cycles; //break;
            case 0xC4: SET(ref cycles, IncsType.R8, 0x01, "H"); return cycles; //break;
            case 0xC5: SET(ref cycles, IncsType.R8, 0x01, "L"); return cycles; //break;
            case 0xC6: SET(ref cycles, IncsType.MAddr, 0x01); return cycles; //break;
            case 0xC7: SET(ref cycles, IncsType.R8, 0x01, "A"); return cycles; //break;
            case 0xC8: SET(ref cycles, IncsType.R8, 0x02, "B"); return cycles; //break;
            case 0xC9: SET(ref cycles, IncsType.R8, 0x02, "C"); return cycles; //break;
            case 0xCA: SET(ref cycles, IncsType.R8, 0x02, "D"); return cycles; //break;
            case 0xCB: SET(ref cycles, IncsType.R8, 0x02, "E"); return cycles; //break;
            case 0xCC: SET(ref cycles, IncsType.R8, 0x02, "H"); return cycles; //break;
            case 0xCD: SET(ref cycles, IncsType.R8, 0x02, "L"); return cycles; //break;
            case 0xCE: SET(ref cycles, IncsType.MAddr, 0x02); return cycles; //break;
            case 0xCF: SET(ref cycles, IncsType.R8, 0x02, "A"); return cycles; //break;

            // 0xD0 ~ 0xDF
            case 0xD0: SET(ref cycles, IncsType.R8, 0x04, "B"); return cycles; //break;
            case 0xD1: SET(ref cycles, IncsType.R8, 0x04, "C"); return cycles; //break;
            case 0xD2: SET(ref cycles, IncsType.R8, 0x04, "D"); return cycles; //break;
            case 0xD3: SET(ref cycles, IncsType.R8, 0x04, "E"); return cycles; //break;
            case 0xD4: SET(ref cycles, IncsType.R8, 0x04, "H"); return cycles; //break;
            case 0xD5: SET(ref cycles, IncsType.R8, 0x04, "L"); return cycles; //break;
            case 0xD6: SET(ref cycles, IncsType.MAddr, 0x04); return cycles; //break;
            case 0xD7: SET(ref cycles, IncsType.R8, 0x04, "A"); return cycles; //break;
            case 0xD8: SET(ref cycles, IncsType.R8, 0x08, "B"); return cycles; //break;
            case 0xD9: SET(ref cycles, IncsType.R8, 0x08, "C"); return cycles; //break;
            case 0xDA: SET(ref cycles, IncsType.R8, 0x08, "D"); return cycles; //break;
            case 0xDB: SET(ref cycles, IncsType.R8, 0x08, "E"); return cycles; //break;
            case 0xDC: SET(ref cycles, IncsType.R8, 0x08, "H"); return cycles; //break;
            case 0xDD: SET(ref cycles, IncsType.R8, 0x08, "L"); return cycles; //break;
            case 0xDE: SET(ref cycles, IncsType.MAddr, 0x08); return cycles; //break;
            case 0xDF: SET(ref cycles, IncsType.R8, 0x08, "A"); return cycles; //break;

            // 0xE0 ~ 0xEF
            case 0xE0: SET(ref cycles, IncsType.R8, 0x10, "B"); return cycles; //break;
            case 0xE1: SET(ref cycles, IncsType.R8, 0x10, "C"); return cycles; //break;
            case 0xE2: SET(ref cycles, IncsType.R8, 0x10, "D"); return cycles; //break;
            case 0xE3: SET(ref cycles, IncsType.R8, 0x10, "E"); return cycles; //break;
            case 0xE4: SET(ref cycles, IncsType.R8, 0x10, "H"); return cycles; //break;
            case 0xE5: SET(ref cycles, IncsType.R8, 0x10, "L"); return cycles; //break;
            case 0xE6: SET(ref cycles, IncsType.MAddr, 0x10); return cycles; //break;
            case 0xE7: SET(ref cycles, IncsType.R8, 0x10, "A"); return cycles; //break;
            case 0xE8: SET(ref cycles, IncsType.R8, 0x20, "B"); return cycles; //break;
            case 0xE9: SET(ref cycles, IncsType.R8, 0x20, "C"); return cycles; //break;
            case 0xEA: SET(ref cycles, IncsType.R8, 0x20, "D"); return cycles; //break;
            case 0xEB: SET(ref cycles, IncsType.R8, 0x20, "E"); return cycles; //break;
            case 0xEC: SET(ref cycles, IncsType.R8, 0x20, "H"); return cycles; //break;
            case 0xED: SET(ref cycles, IncsType.R8, 0x20, "L"); return cycles; //break;
            case 0xEE: SET(ref cycles, IncsType.MAddr, 0x20); return cycles; //break;
            case 0xEF: SET(ref cycles, IncsType.R8, 0x20, "A"); return cycles; //break;

            // 0xF0 ~ 0xFF
            case 0xF0: SET(ref cycles, IncsType.R8, 0x40, "B"); return cycles; //break;
            case 0xF1: SET(ref cycles, IncsType.R8, 0x40, "C"); return cycles; //break;
            case 0xF2: SET(ref cycles, IncsType.R8, 0x40, "D"); return cycles; //break;
            case 0xF3: SET(ref cycles, IncsType.R8, 0x40, "E"); return cycles; //break;
            case 0xF4: SET(ref cycles, IncsType.R8, 0x40, "H"); return cycles; //break;
            case 0xF5: SET(ref cycles, IncsType.R8, 0x40, "L"); return cycles; //break;
            case 0xF6: SET(ref cycles, IncsType.MAddr, 0x40); return cycles; //break;
            case 0xF7: SET(ref cycles, IncsType.R8, 0x40, "A"); return cycles; //break;
            case 0xF8: SET(ref cycles, IncsType.R8, 0x80, "B"); return cycles; //break;
            case 0xF9: SET(ref cycles, IncsType.R8, 0x80, "C"); return cycles; //break;
            case 0xFA: SET(ref cycles, IncsType.R8, 0x80, "D"); return cycles; //break;
            case 0xFB: SET(ref cycles, IncsType.R8, 0x80, "E"); return cycles; //break;
            case 0xFC: SET(ref cycles, IncsType.R8, 0x80, "H"); return cycles; //break;
            case 0xFD: SET(ref cycles, IncsType.R8, 0x80, "L"); return cycles; //break;
            case 0xFE: SET(ref cycles, IncsType.MAddr, 0x80); return cycles; //break;
            case 0xFF: SET(ref cycles, IncsType.R8, 0x80, "A"); return cycles; //break;

            default: warnUnsupportedOpcode(opcode); break;
        }
        cycles += _cbIncCycles[opcode];
        return cycles;
    }

    private void SET(ref int _cycles, IncsType incsType, u8 bit, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            switch (data1)
            {
                case "B": B = (u8) (B | bit); break;
                case "C": C = (u8) (C | bit); break;
                case "D": D = (u8) (D | bit); break;
                case "E": E = (u8) (E | bit); break;
                case "H": H = (u8) (H | bit); break;
                case "L": L = (u8) (L | bit); break;
                case "A": A = (u8) (A | bit); break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            _mmu.Write(HL, (u8) (_mmu.Read(HL) | bit));
            _cycles += 16;
        }
    }
    private void RES(ref int _cycles, IncsType incsType, u8 bit, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            switch (data1)
            {
                case "B": B = (u8) (B & ~bit); break;
                case "C": C = (u8) (C & ~bit); break;
                case "D": D = (u8) (D & ~bit); break;
                case "E": E = (u8) (E & ~bit); break;
                case "H": H = (u8) (H & ~bit); break;
                case "L": L = (u8) (L & ~bit); break;
                case "A": A = (u8) (A & ~bit); break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            _mmu.Write(HL, (u8) (_mmu.Read(HL) & ~bit));
            _cycles += 16;
        }
    }

    private void BIT(ref int _cycles, IncsType incsType, u8 bit, string data1 = "")
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

            FlagZ = (value & bit) == 0;
            FlagN = false;
            FlagH = true;

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            FlagZ = (value & bit) == 0;
            FlagN = false;
            FlagH = true;

            _cycles += 12;
        }
    }

    private void DI(ref int _cycles)
    {
        IME = false;
        _cycles += 4;
    }
    private void EI(ref int _cycles)
    {
        IMEEnabler = true;
        _cycles += 4;
    }

    private void SRL(ref int _cycles, IncsType incsType, string data1 = "")
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

            u8 result = (u8) (value >> 1);

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;

            switch (data1)
            {
                case "B": B = result; break;
                case "C": C = result; break;
                case "D": D = result; break;
                case "E": E = result; break;
                case "H": H = result; break;
                case "L": L = result; break;
                case "A": A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            u8 result = (u8) (value >> 1);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;
            _mmu.Write(HL, result);

            _cycles += 16;
        }
    }

private void SWAP(ref int _cycles, IncsType incsType, string data1 = "")
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

            u8 result = (u8) (((value & 0xF0) >> 4) | ((value & 0x0F) << 4));

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;

            switch (data1)
            {
                case "B": B = result; break;
                case "C": C = result; break;
                case "D": D = result; break;
                case "E": E = result; break;
                case "H": H = result; break;
                case "L": L = result; break;
                case "A": A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            u8 result = (u8) (((value & 0xF0) >> 4) | ((value & 0x0F) << 4));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            _mmu.Write(HL, result);

            _cycles += 16;
        }
    }
    private void SRA(ref int _cycles, IncsType incsType, string data1 = "")
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

            u8 result = (u8) ((value >> 1) | (value & 0x80));

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;

            switch (data1)
            {
                case "B": B = result; break;
                case "C": C = result; break;
                case "D": D = result; break;
                case "E": E = result; break;
                case "H": H = result; break;
                case "L": L = result; break;
                case "A": A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            u8 result = (u8) ((value >> 1) | (value & 0x80));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;
            _mmu.Write(HL, result);

            _cycles += 16;
        }
    }

    private void SLA(ref int _cycles, IncsType incsType, string data1 = "")
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

            u8 result = (u8) (value << 1);

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x80) != 0;

            switch (data1)
            {
                case "B": B = result; break;
                case "C": C = result; break;
                case "D": D = result; break;
                case "E": E = result; break;
                case "H": H = result; break;
                case "L": L = result; break;
                case "A": A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ((u8) (value << 1));
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x80) != 0;
            _mmu.Write(HL, (u8) (value << 1));

            _cycles += 16;
        }
    }

    private void RR(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            bool oldC = FlagC;
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

            u8 result = (u8) ((value >> 1) | (oldC ? 0x80 : 0));

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;

            switch (data1)
            {
                case "B": B = result; break;
                case "C": C = result; break;
                case "D": D = result; break;
                case "E": E = result; break;
                case "H": H = result; break;
                case "L": L = result; break;
                case "A": A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            bool oldC = FlagC;
            u8 value = _mmu.Read(HL);
            u8 result = (u8) ((value >> 1) | (oldC ? 0x80 : 0));

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;
            _mmu.Write(HL, result);

            _cycles += 16;
        }
    } 

    private void RL(ref int _cycles, IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            bool oldC = FlagC;
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

            u8 result = (u8) ((value << 1) | (oldC ? 1 : 0));

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x80) != 0;

            switch (data1)
            {
                case "B": B = result; break;
                case "C": C = result; break;
                case "D": D = result; break;
                case "E": E = result; break;
                case "H": H = result; break;
                case "L": L = result; break;
                case "A": A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            bool oldC = FlagC;
            u8 value = _mmu.Read(HL);
            u8 result = (u8) ((value << 1) | (oldC ? 1 : 0));

            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x80) != 0;
            _mmu.Write(HL, result);

            _cycles += 16;
        }
    }

    private void RRC(ref int _cycles, IncsType incsType, string data1 = "")
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

            SetFlagZ((u8) ((value >> 1) | (value << 7)));
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;

            switch (data1)
            {
                case "B": B = (u8) ((value >> 1) | (value << 7)); break;
                case "C": C = (u8) ((value >> 1) | (value << 7)); break;
                case "D": D = (u8) ((value >> 1) | (value << 7)); break;
                case "E": E = (u8) ((value >> 1) | (value << 7)); break;
                case "H": H = (u8) ((value >> 1) | (value << 7)); break;
                case "L": L = (u8) ((value >> 1) | (value << 7)); break;
                case "A": A = (u8) ((value >> 1) | (value << 7)); break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ((u8) ((value >> 1) | (value << 7)));
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x01) != 0;
            _mmu.Write(HL, (u8) ((value >> 1) | (value << 7)));

            _cycles += 16;
        }
    }

    private void RLC(ref int _cycles, IncsType incsType, string data1 = "")
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

            SetFlagZ((u8) ((value << 1) | (value >> 7)));
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x80) != 0;

            switch (data1)
            {
                case "B": B = (u8) ((value << 1) | (value >> 7)); break;
                case "C": C = (u8) ((value << 1) | (value >> 7)); break;
                case "D": D = (u8) ((value << 1) | (value >> 7)); break;
                case "E": E = (u8) ((value << 1) | (value >> 7)); break;
                case "H": H = (u8) ((value << 1) | (value >> 7)); break;
                case "L": L = (u8) ((value << 1) | (value >> 7)); break;
                case "A": A = (u8) ((value << 1) | (value >> 7)); break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(HL);
            SetFlagZ((u8) ((value << 1) | (value >> 7)));
            FlagN = false;
            FlagH = false;
            FlagC = (value & 0x80) != 0;
            _mmu.Write(HL, (u8) ((value << 1) | (value >> 7)));

            _cycles += 16;
        }
    }
    private void JR(ref int _cycles, bool state)
    {
        if (state)
        {
            sbyte sb = (sbyte) _mmu.Read(PC);
            PC = (u16) (PC + sb);
            PC += 1;
            _cycles += 12;
        }
        else
        {
            PC += 1;
            _cycles += 8;
        }
    }

    private void JR2(bool flag) {
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

    private void INC(ref int _cycles, IncsType incsType, string data)
    {
        if (incsType == IncsType.R16)
        {
            switch (data)
            {
                case "BC": BC += 1; break;
                case "DE": DE += 1; break;
                case "HL": HL += 1; break;
                case "SP": SP += 1; break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data)
            {
                case "B": value = B; B += 1; break;
                case "D": value = D; D += 1; break;
                case "H": value = H; H += 1; break;
                case "C": value = C; C += 1; break;
                case "E": value = E; E += 1; break;
                case "L": value = L; L += 1; break;
                case "A": value = A; A += 1; break;
            }

            // set flag
            SetFlagZ(value + 1);
            FlagN = false;
            SetFlagH(value, 1);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            if (data == "HL")
            {
                u8 value = _mmu.Read(HL);
                _mmu.Write(HL, (u8) (value + 1));
                // set flag
                SetFlagZ(value + 1);
                FlagN = false;
                SetFlagH(value, 1);
                _cycles += 12;
            }
        }
    }

    private byte INC2(byte b) { //Z0H-
        int result = b + 1;
        SetFlagZ(result);
        FlagN = false;
        SetFlagH(b, 1);
        return (byte)result;
    }
    private void DEC(ref int _cycles, IncsType incsType, string data)
    {
        if (incsType == IncsType.R16)
        {
            switch (data)
            {
                case "BC": BC -= 1; break;
                case "DE": DE -= 1; break;
                case "HL": HL -= 1; break;
                case "SP": SP -= 1; break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data)
            {
                case "B": value = B; B -= 1; break;
                case "D": value = D; D -= 1; break;
                case "H": value = H; H -= 1; break;
                case "C": value = C; C -= 1; break;
                case "E": value = E; E -= 1; break;
                case "L": value = L; L -= 1; break;
                case "A": value = A; A -= 1; break;
            }

            // set flag
            SetFlagZ(value - 1);
            FlagN = true;
            SetFlagHSub(value, 1);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            if (data == "HL")
            {
                u8 value = _mmu.Read(HL);
                _mmu.Write(HL, (u8) (value - 1));
                // set flag
                SetFlagZ(value - 1);
                FlagN = true;
                SetFlagHSub(value, 1);
                _cycles += 12;
            }
        }
    }

    private byte DEC2(byte b) { //Z1H-
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

    private void CALL(ref int _cycles, bool flag)
    {
        if (flag)
        {
            PUSH(ref _cycles, "", IncsType.A16, (u16) (PC + 2));
            _cycles -= 16; // PUSH Cycle
            PC = _mmu.ReadROM16(PC);
            _cycles += 24;
        }
        else
        {
            PC += 2;
            _cycles += 12;
        }
    }

    private void PUSH(ref int _cycles, string data1, IncsType incsType = IncsType.R16, u16 number = 0)
    {
        if (incsType == IncsType.R16)
        {
            u16 value = 0;
            switch (data1)
            {
                case "BC": value = BC; break;
                case "DE": value = DE; break;
                case "HL": value = HL; break;
                case "AF": value = AF; break;
            }

            SP -= 2;
            _mmu.WriteROM16(SP, value);
            _cycles += 16;
        }
        else if (incsType == IncsType.A16)
        {
            SP -= 2;
            _mmu.WriteROM16(SP, number);
            _cycles += 16;
        }
        else if (incsType == IncsType.NO_CYCLE)
        {
            SP -= 2;
            _mmu.WriteROM16(SP, number);
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


    private void RST(ref int _cycles, u8 value)
    {
        PUSH(ref _cycles, "", IncsType.A16, PC);
        PC = value;
        _cycles -= 16; // PUSH Cycle
        _cycles += 16;
    }


    private void HALT(ref int _cycles)
    {
        if (!IME && ((_mmu.GetIE() & _mmu.IFRegister & 0x1F) == 0)) { HALTED = true; PC --;}
        _cycles += 4;
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
            // PUSH2(PC);
            SP -= 2;
            _mmu.WriteROM16(SP, PC);
            PC = (ushort)(0x40 + (8 * b));
            IME = false;
            
            _mmu.IFRegister = BitClear(b, _mmu.IFRegister);
        }
    }

    private u8 BitClear(int n, u8 value)
    {//b, _mmu.GetIF()
        return value &= (u8)~(1 << n);
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