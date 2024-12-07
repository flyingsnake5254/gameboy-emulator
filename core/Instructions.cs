public class Instructions
{
    private bool _ime;
    private bool _isHalted;
    private bool _enablingIME;
    private Registers _regs;
    private MMU _mmu;
    private int _cycles;
    

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
    public Instructions(ref Registers regs, ref MMU mmu)
    {
        this._regs = regs;
        this._mmu = mmu;
    }

    public int Execute(u8 opcode)
    {
        _cycles = 0;
        switch(opcode)
        {
            // 0x00 ~ 0x0F
            case 0x00: NOP(); break;
            case 0x01: LD(IncsType.R16_D16, "BC"); break;
            case 0x02: LD(IncsType.MAddr_R8, "BC", "A"); break;
            case 0x03: INC(IncsType.R16, "BC"); break;
            case 0x04: INC(IncsType.R8, "B"); break;
            case 0x05: DEC(IncsType.R8, "B"); break;
            case 0x06: LD(IncsType.R8_D8, "B"); break;
            case 0x07: RLCA(); break;
            case 0x08: LD(IncsType.A16_R16); break;
            case 0x09: ADD(IncsType.R16_R16, "BC"); break;
            case 0x0A: LD(IncsType.R8_MAddr, "A", "BC"); break;
            case 0x0B: DEC(IncsType.R16, "BC"); break;
            case 0x0C: INC(IncsType.R8, "C"); break;
            case 0x0D: DEC(IncsType.R8, "C"); break;
            case 0x0E: LD(IncsType.R8_D8, "C"); break;
            case 0x0F: RRCA(); break;

            // 0x10 ~ 0x1F
            case 0x10: STOP(); break;
            case 0x11: LD(IncsType.R16_D16, "DE"); break;
            case 0x12: LD(IncsType.MAddr_R8, "DE", "A"); break;
            case 0x13: INC(IncsType.R16, "DE"); break;
            case 0x14: INC(IncsType.R8, "D"); break;
            case 0x15: DEC(IncsType.R8, "D"); break;
            case 0x16: LD(IncsType.R8_D8, "D"); break;
            case 0x17: RLA(); break;
            case 0x18: JR(true); break;
            case 0x19: ADD(IncsType.R16_R16, "DE"); break;
            case 0x1A: LD(IncsType.R8_MAddr, "A", "DE"); break;
            case 0x1B: DEC(IncsType.R16, "DE"); break;
            case 0x1C: INC(IncsType.R8, "E"); break;
            case 0x1D: DEC(IncsType.R8, "E"); break;
            case 0x1E: LD(IncsType.R8_D8, "E"); break;
            case 0x1F: RRA(); break;

            // 0x20 ~ 0x2F
            case 0x20: JR(!_regs.GetFlag(Registers.Flag.Z)); break;
            case 0x21: LD(IncsType.R16_D16, "HL"); break;
            case 0x22: LD(IncsType.MAddr_R8, "HL+", "A"); break;
            case 0x23: INC(IncsType.R16, "HL"); break;
            case 0x24: INC(IncsType.R8, "H"); break;
            case 0x25: DEC(IncsType.R8, "H"); break;
            case 0x26: LD(IncsType.R8_D8, "H"); break;
            case 0x27: DAA(); break;
            case 0x28: JR(_regs.GetFlag(Registers.Flag.Z)); break;
            case 0x29: ADD(IncsType.R16_R16, "HL"); break;
            case 0x2A: LD(IncsType.R8_MAddr, "A", "HL+"); break;
            case 0x2B: DEC(IncsType.R16, "HL"); break;
            case 0x2C: INC(IncsType.R8, "L"); break;
            case 0x2D: DEC(IncsType.R8, "L"); break;
            case 0x2E: LD(IncsType.R8_D8, "L"); break;
            case 0x2F: CPL(); break;

            // 0x30 ~ 0x3F
            case 0x30: JR(!_regs.GetFlag(Registers.Flag.C)); break;
            case 0x31: LD(IncsType.R16_D16, "SP"); break;
            case 0x32: LD(IncsType.MAddr_R8, "HL-", "A"); break;
            case 0x33: INC(IncsType.R16, "SP"); break;
            case 0x34: INC(IncsType.MAddr, "HL"); break;
            case 0x35: DEC(IncsType.MAddr, "HL"); break;
            case 0x36: LD(IncsType.MAddr_D8); break;
            case 0x37: SCF(); break;
            case 0x38: JR(_regs.GetFlag(Registers.Flag.C)); break;
            case 0x39: ADD(IncsType.R16_R16, "SP"); break;
            case 0x3A: LD(IncsType.R8_MAddr, "A", "HL-"); break;
            case 0x3B: DEC(IncsType.R16, "SP"); break;
            case 0x3C: INC(IncsType.R8, "A"); break;
            case 0x3D: DEC(IncsType.R8, "A"); break;
            case 0x3E: LD(IncsType.R8_D8, "A"); break;
            case 0x3F: CCF(); break;

            // 0x40 ~ 0x4F
            case 0x40: LD(IncsType.R8_R8, "B", "B"); break;
            case 0x41: LD(IncsType.R8_R8, "B", "C"); break;
            case 0x42: LD(IncsType.R8_R8, "B", "D"); break;
            case 0x43: LD(IncsType.R8_R8, "B", "E"); break;
            case 0x44: LD(IncsType.R8_R8, "B", "H"); break;
            case 0x45: LD(IncsType.R8_R8, "B", "L"); break;
            case 0x46: LD(IncsType.R8_MAddr, "B", "HL"); break;
            case 0x47: LD(IncsType.R8_R8, "B", "A"); break;
            case 0x48: LD(IncsType.R8_R8, "C", "B"); break;
            case 0x49: LD(IncsType.R8_R8, "C", "C"); break;
            case 0x4A: LD(IncsType.R8_R8, "C", "D"); break;
            case 0x4B: LD(IncsType.R8_R8, "C", "E"); break;
            case 0x4C: LD(IncsType.R8_R8, "C", "H"); break;
            case 0x4D: LD(IncsType.R8_R8, "C", "L"); break;
            case 0x4E: LD(IncsType.R8_MAddr, "C", "HL"); break;
            case 0x4F: LD(IncsType.R8_R8, "C", "A"); break;

            // 0x50 ~ 0x5F
            case 0x50: LD(IncsType.R8_R8, "D", "B"); break;
            case 0x51: LD(IncsType.R8_R8, "D", "C"); break;
            case 0x52: LD(IncsType.R8_R8, "D", "D"); break;
            case 0x53: LD(IncsType.R8_R8, "D", "E"); break;
            case 0x54: LD(IncsType.R8_R8, "D", "H"); break;
            case 0x55: LD(IncsType.R8_R8, "D", "L"); break;
            case 0x56: LD(IncsType.R8_MAddr, "D", "HL"); break;
            case 0x57: LD(IncsType.R8_R8, "D", "A"); break;
            case 0x58: LD(IncsType.R8_R8, "E", "B"); break;
            case 0x59: LD(IncsType.R8_R8, "E", "C"); break;
            case 0x5A: LD(IncsType.R8_R8, "E", "D"); break;
            case 0x5B: LD(IncsType.R8_R8, "E", "E"); break;
            case 0x5C: LD(IncsType.R8_R8, "E", "H"); break;
            case 0x5D: LD(IncsType.R8_R8, "E", "L"); break;
            case 0x5E: LD(IncsType.R8_MAddr, "E", "HL"); break;
            case 0x5F: LD(IncsType.R8_R8, "E", "A"); break;

            // 0x60 ~ 0x6F
            case 0x60: LD(IncsType.R8_R8, "H", "B"); break;
            case 0x61: LD(IncsType.R8_R8, "H", "C"); break;
            case 0x62: LD(IncsType.R8_R8, "H", "D"); break;
            case 0x63: LD(IncsType.R8_R8, "H", "E"); break;
            case 0x64: LD(IncsType.R8_R8, "H", "H"); break;
            case 0x65: LD(IncsType.R8_R8, "H", "L"); break;
            case 0x66: LD(IncsType.R8_MAddr, "H", "HL"); break;
            case 0x67: LD(IncsType.R8_R8, "H", "A"); break;
            case 0x68: LD(IncsType.R8_R8, "L", "B"); break;
            case 0x69: LD(IncsType.R8_R8, "L", "C"); break;
            case 0x6A: LD(IncsType.R8_R8, "L", "D"); break;
            case 0x6B: LD(IncsType.R8_R8, "L", "E"); break;
            case 0x6C: LD(IncsType.R8_R8, "L", "H"); break;
            case 0x6D: LD(IncsType.R8_R8, "L", "L"); break;
            case 0x6E: LD(IncsType.R8_MAddr, "L", "HL"); break;
            case 0x6F: LD(IncsType.R8_R8, "L", "A"); break;

            // 0x70 ~ 0x7F
            case 0x70: LD(IncsType.MAddr_R8, "HL", "B"); break;
            case 0x71: LD(IncsType.MAddr_R8, "HL", "C"); break;
            case 0x72: LD(IncsType.MAddr_R8, "HL", "D"); break;
            case 0x73: LD(IncsType.MAddr_R8, "HL", "E"); break;
            case 0x74: LD(IncsType.MAddr_R8, "HL", "H"); break;
            case 0x75: LD(IncsType.MAddr_R8, "HL", "L"); break;
            case 0x76: HALT(); break;
            case 0x77: LD(IncsType.MAddr_R8, "HL", "A"); break;
            case 0x78: LD(IncsType.R8_R8, "A", "B"); break;
            case 0x79: LD(IncsType.R8_R8, "A", "C"); break;
            case 0x7A: LD(IncsType.R8_R8, "A", "D"); break;
            case 0x7B: LD(IncsType.R8_R8, "A", "E"); break;
            case 0x7C: LD(IncsType.R8_R8, "A", "H"); break;
            case 0x7D: LD(IncsType.R8_R8, "A", "L"); break;
            case 0x7E: LD(IncsType.R8_MAddr, "A", "HL"); break;
            case 0x7F: LD(IncsType.R8_R8, "A", "A"); break;

            // 0x80 ~ 0x8F
            case 0x80: ADD(IncsType.R8_R8, "B"); break;
            case 0x81: ADD(IncsType.R8_R8, "C"); break;
            case 0x82: ADD(IncsType.R8_R8, "D"); break;
            case 0x83: ADD(IncsType.R8_R8, "E"); break;
            case 0x84: ADD(IncsType.R8_R8, "H"); break;
            case 0x85: ADD(IncsType.R8_R8, "L"); break;
            case 0x86: ADD(IncsType.R8_MAddr); break;
            case 0x87: ADD(IncsType.R8_R8, "A"); break;
            case 0x88: ADC(IncsType.R8_R8, "B"); break;
            case 0x89: ADC(IncsType.R8_R8, "C"); break;
            case 0x8A: ADC(IncsType.R8_R8, "D"); break;
            case 0x8B: ADC(IncsType.R8_R8, "E"); break;
            case 0x8C: ADC(IncsType.R8_R8, "H"); break;
            case 0x8D: ADC(IncsType.R8_R8, "L"); break;
            case 0x8E: ADC(IncsType.R8_MAddr); break;
            case 0x8F: ADC(IncsType.R8_R8, "A"); break;
            

            // 0x90 ~ 0x9F
            case 0x90: SUB(IncsType.R8, "B"); break;
            case 0x91: SUB(IncsType.R8, "C"); break;
            case 0x92: SUB(IncsType.R8, "D"); break;
            case 0x93: SUB(IncsType.R8, "E"); break;
            case 0x94: SUB(IncsType.R8, "H"); break;
            case 0x95: SUB(IncsType.R8, "L"); break;
            case 0x96: SUB(IncsType.MAddr); break;
            case 0x97: SUB(IncsType.R8, "A"); break;
            case 0x98: SBC(IncsType.R8_R8, "B"); break;
            case 0x99: SBC(IncsType.R8_R8, "C"); break;
            case 0x9A: SBC(IncsType.R8_R8, "D"); break;
            case 0x9B: SBC(IncsType.R8_R8, "E"); break;
            case 0x9C: SBC(IncsType.R8_R8, "H"); break;
            case 0x9D: SBC(IncsType.R8_R8, "L"); break;
            case 0x9E: SBC(IncsType.R8_MAddr); break;
            case 0x9F: SBC(IncsType.R8_R8, "A"); break;

            // 0xA0 ~ 0xAF
            case 0xA0: AND(IncsType.R8, "B"); break;
            case 0xA1: AND(IncsType.R8, "C"); break;
            case 0xA2: AND(IncsType.R8, "D"); break;
            case 0xA3: AND(IncsType.R8, "E"); break;
            case 0xA4: AND(IncsType.R8, "H"); break;
            case 0xA5: AND(IncsType.R8, "L"); break;
            case 0xA6: AND(IncsType.MAddr); break;
            case 0xA7: AND(IncsType.R8, "A"); break;
            case 0xA8: XOR(IncsType.R8, "B"); break;
            case 0xA9: XOR(IncsType.R8, "C"); break;
            case 0xAA: XOR(IncsType.R8, "D"); break;
            case 0xAB: XOR(IncsType.R8, "E"); break;
            case 0xAC: XOR(IncsType.R8, "H"); break;
            case 0xAD: XOR(IncsType.R8, "L"); break;
            case 0xAE: XOR(IncsType.MAddr); break;
            case 0xAF: XOR(IncsType.R8, "A"); break;

            // 0xB0 ~ 0xBF
            case 0xB0: OR(IncsType.R8, "B"); break;
            case 0xB1: OR(IncsType.R8, "C"); break;
            case 0xB2: OR(IncsType.R8, "D"); break;
            case 0xB3: OR(IncsType.R8, "E"); break;
            case 0xB4: OR(IncsType.R8, "H"); break;
            case 0xB5: OR(IncsType.R8, "L"); break;
            case 0xB6: OR(IncsType.MAddr); break;
            case 0xB7: OR(IncsType.R8, "A"); break;
            case 0xB8: CP(IncsType.R8, "B"); break;
            case 0xB9: CP(IncsType.R8, "C"); break;
            case 0xBA: CP(IncsType.R8, "D"); break;
            case 0xBB: CP(IncsType.R8, "E"); break;
            case 0xBC: CP(IncsType.R8, "H"); break;
            case 0xBD: CP(IncsType.R8, "L"); break;
            case 0xBE: CP(IncsType.MAddr); break;
            case 0xBF: CP(IncsType.R8, "A"); break;

            // 0xC0 ~ 0xCF
            case 0xC0: RET(!_regs.GetFlag(Registers.Flag.Z)); break;
            case 0xC1: POP("BC"); break;
            case 0xC2: JP(!_regs.GetFlag(Registers.Flag.Z)); break;
            case 0xC3: JP(true); break;
            case 0xC4: CALL(!_regs.GetFlag(Registers.Flag.Z)); break;
            case 0xC5: PUSH("BC"); break;
            case 0xC6: ADD(IncsType.R8_D8); break;
            case 0xC7: RST(0x0); break;
            case 0xC8: RET(_regs.GetFlag(Registers.Flag.Z)); break;
            case 0xC9: RET(true); break;
            case 0xCA: JP(_regs.GetFlag(Registers.Flag.Z)); break;
            case 0xCB: PrefixCB(); break;
            case 0xCC: CALL(_regs.GetFlag(Registers.Flag.Z)); break;
            case 0xCD: CALL(true); break;
            case 0xCE: ADC(IncsType.R8_D8); break;
            case 0xCF: RST(0x08); break;

            // 0xD0 ~ 0xDF
            case 0xD0: RET(!_regs.GetFlag(Registers.Flag.C)); break;
            case 0xD1: POP("DE"); break;
            case 0xD2: JP(!_regs.GetFlag(Registers.Flag.C)); break;
            case 0xD4: CALL(!_regs.GetFlag(Registers.Flag.C)); break;
            case 0xD5: PUSH("DE"); break;
            case 0xD6: SUB(IncsType.D8); break;
            case 0xD7: RST(0x10); break;
            case 0xD8: RET(_regs.GetFlag(Registers.Flag.C)); break;
            case 0xD9: RETI(); break;
            case 0xDA: JP(_regs.GetFlag(Registers.Flag.C)); break;
            case 0xDC: CALL(_regs.GetFlag(Registers.Flag.C)); break;
            case 0xDE: SBC(IncsType.R8_D8); break;
            case 0xDF: RST(0x18); break;

            // 0xE0 ~ 0xEF
            case 0xE0: LD(IncsType.A8_R8); break;
            case 0xE1: POP("HL"); break;
            case 0xE2: LD(IncsType.MAddr8_R8); break;
            case 0xE5: PUSH("HL"); break;
            case 0xE6: AND(IncsType.D8); break;
            case 0xE7: RST(0x20); break;
            case 0xE8: ADD(IncsType.R16_S8); break;
            case 0xE9: JP(true, IncsType.MAddr); break;
            case 0xEA: LD(IncsType.A16_R8); break;
            case 0xEE: XOR(IncsType.D8); break;
            case 0xEF: RST(0x28); break;

            // 0xF0 ~ 0xFF
            case 0xF0: LD(IncsType.R8_A8); break;
            case 0xF1: POP("AF"); break;
            case 0xF2: LD(IncsType.R8_MAddr8); break;
            case 0xF3: DI(); break;
            case 0xF5: PUSH("AF"); break;
            case 0xF6: OR(IncsType.D8); break;
            case 0xF7: RST(0x30); break;
            case 0xF8: LD(IncsType.R16_R16S8); break;
            case 0xF9: LD(IncsType.R16_R16); break;
            case 0xFA: LD(IncsType.R8_A16); break;
            case 0xFB: EI(); break;
            case 0xFE: CP(IncsType.D8); break;
            case 0xFF: RST(0x38); break;
            
        }
        return _cycles;
    }

    public void UpdateIME()
    {
        _ime |= _enablingIME;
        _enablingIME = false;
    }
    public void Interrupt (int value)
    {
        if (_isHalted)
        {
            _regs.PC ++;
            _isHalted = false;
        }

        if (_ime)
        {
            PUSH("", IncsType.NO_CYCLE, _regs.PC);
            _regs.PC = (u16) (0x40 + (8 * value));
            _ime = false;

            _mmu.IFRegister = (u8) (_mmu.IFRegister & ((u8) ~(1 << value)));
        }
    }
    
    private void PrefixCB()
    {
        u8 opcode = _mmu.Read(_regs.PC ++);

        switch (opcode)
        {
            // 0x00 ~ 0x0F
            case 0x00: RLC(IncsType.R8, "B"); break;
            case 0x01: RLC(IncsType.R8, "C"); break;
            case 0x02: RLC(IncsType.R8, "D"); break;
            case 0x03: RLC(IncsType.R8, "E"); break;
            case 0x04: RLC(IncsType.R8, "H"); break;
            case 0x05: RLC(IncsType.R8, "L"); break;
            case 0x06: RLC(IncsType.MAddr); break;
            case 0x07: RLC(IncsType.R8, "A"); break;
            case 0x08: RRC(IncsType.R8, "B"); break;
            case 0x09: RRC(IncsType.R8, "C"); break;
            case 0x0A: RRC(IncsType.R8, "D"); break;
            case 0x0B: RRC(IncsType.R8, "E"); break;
            case 0x0C: RRC(IncsType.R8, "H"); break;
            case 0x0D: RRC(IncsType.R8, "L"); break;
            case 0x0E: RRC(IncsType.MAddr); break;
            case 0x0F: RRC(IncsType.R8, "A"); break;

            // 0x10 ~ 0x1F
            case 0x10: RL(IncsType.R8, "B"); break;
            case 0x11: RL(IncsType.R8, "C"); break;
            case 0x12: RL(IncsType.R8, "D"); break;
            case 0x13: RL(IncsType.R8, "E"); break;
            case 0x14: RL(IncsType.R8, "H"); break;
            case 0x15: RL(IncsType.R8, "L"); break;
            case 0x16: RL(IncsType.MAddr); break;
            case 0x17: RL(IncsType.R8, "A"); break;
            case 0x18: RR(IncsType.R8, "B"); break;
            case 0x19: RR(IncsType.R8, "C"); break;
            case 0x1A: RR(IncsType.R8, "D"); break;
            case 0x1B: RR(IncsType.R8, "E"); break;
            case 0x1C: RR(IncsType.R8, "H"); break;
            case 0x1D: RR(IncsType.R8, "L"); break;
            case 0x1E: RR(IncsType.MAddr); break;
            case 0x1F: RR(IncsType.R8, "A"); break;

            // 0x20 ~ 0x2F
            case 0x20: SLA(IncsType.R8, "B"); break;
            case 0x21: SLA(IncsType.R8, "C"); break;
            case 0x22: SLA(IncsType.R8, "D"); break;
            case 0x23: SLA(IncsType.R8, "E"); break;
            case 0x24: SLA(IncsType.R8, "H"); break;
            case 0x25: SLA(IncsType.R8, "L"); break;
            case 0x26: SLA(IncsType.MAddr); break;
            case 0x27: SLA(IncsType.R8, "A"); break;
            case 0x28: SRA(IncsType.R8, "B"); break;
            case 0x29: SRA(IncsType.R8, "C"); break;
            case 0x2A: SRA(IncsType.R8, "D"); break;
            case 0x2B: SRA(IncsType.R8, "E"); break;
            case 0x2C: SRA(IncsType.R8, "H"); break;
            case 0x2D: SRA(IncsType.R8, "L"); break;
            case 0x2E: SRA(IncsType.MAddr); break;
            case 0x2F: SRA(IncsType.R8, "A"); break;

            // 0x30 ~ 0x3F
            case 0x30: SWAP(IncsType.R8, "B"); break;
            case 0x31: SWAP(IncsType.R8, "C"); break;
            case 0x32: SWAP(IncsType.R8, "D"); break;
            case 0x33: SWAP(IncsType.R8, "E"); break;
            case 0x34: SWAP(IncsType.R8, "H"); break;
            case 0x35: SWAP(IncsType.R8, "L"); break;
            case 0x36: SWAP(IncsType.MAddr); break;
            case 0x37: SWAP(IncsType.R8, "A"); break;
            case 0x38: SRL(IncsType.R8, "B"); break;
            case 0x39: SRL(IncsType.R8, "C"); break;
            case 0x3A: SRL(IncsType.R8, "D"); break;
            case 0x3B: SRL(IncsType.R8, "E"); break;
            case 0x3C: SRL(IncsType.R8, "H"); break;
            case 0x3D: SRL(IncsType.R8, "L"); break;
            case 0x3E: SRL(IncsType.MAddr); break;
            case 0x3F: SRL(IncsType.R8, "A"); break;

            // 0x40 ~ 0x4F
            case 0x40: BIT(IncsType.R8, 0x01, "B"); break;
            case 0x41: BIT(IncsType.R8, 0x01, "C"); break;
            case 0x42: BIT(IncsType.R8, 0x01, "D"); break;
            case 0x43: BIT(IncsType.R8, 0x01, "E"); break;
            case 0x44: BIT(IncsType.R8, 0x01, "H"); break;
            case 0x45: BIT(IncsType.R8, 0x01, "L"); break;
            case 0x46: BIT(IncsType.MAddr, 0x01); break;
            case 0x47: BIT(IncsType.R8, 0x01, "A"); break;
            case 0x48: BIT(IncsType.R8, 0x02, "B"); break;
            case 0x49: BIT(IncsType.R8, 0x02, "C"); break;
            case 0x4A: BIT(IncsType.R8, 0x02, "D"); break;
            case 0x4B: BIT(IncsType.R8, 0x02, "E"); break;
            case 0x4C: BIT(IncsType.R8, 0x02, "H"); break;
            case 0x4D: BIT(IncsType.R8, 0x02, "L"); break;
            case 0x4E: BIT(IncsType.MAddr, 0x02); break;
            case 0x4F: BIT(IncsType.R8, 0x02, "A"); break;

            // 0x50 ~ 0x5F
            case 0x50: BIT(IncsType.R8, 0x04, "B"); break;
            case 0x51: BIT(IncsType.R8, 0x04, "C"); break;
            case 0x52: BIT(IncsType.R8, 0x04, "D"); break;
            case 0x53: BIT(IncsType.R8, 0x04, "E"); break;
            case 0x54: BIT(IncsType.R8, 0x04, "H"); break;
            case 0x55: BIT(IncsType.R8, 0x04, "L"); break;
            case 0x56: BIT(IncsType.MAddr, 0x04); break;
            case 0x57: BIT(IncsType.R8, 0x04, "A"); break;
            case 0x58: BIT(IncsType.R8, 0x08, "B"); break;
            case 0x59: BIT(IncsType.R8, 0x08, "C"); break;
            case 0x5A: BIT(IncsType.R8, 0x08, "D"); break;
            case 0x5B: BIT(IncsType.R8, 0x08, "E"); break;
            case 0x5C: BIT(IncsType.R8, 0x08, "H"); break;
            case 0x5D: BIT(IncsType.R8, 0x08, "L"); break;
            case 0x5E: BIT(IncsType.MAddr, 0x08); break;
            case 0x5F: BIT(IncsType.R8, 0x08, "A"); break;

            // 0x60 ~ 0x6F
            case 0x60: BIT(IncsType.R8, 0x10, "B"); break;
            case 0x61: BIT(IncsType.R8, 0x10, "C"); break;
            case 0x62: BIT(IncsType.R8, 0x10, "D"); break;
            case 0x63: BIT(IncsType.R8, 0x10, "E"); break;
            case 0x64: BIT(IncsType.R8, 0x10, "H"); break;
            case 0x65: BIT(IncsType.R8, 0x10, "L"); break;
            case 0x66: BIT(IncsType.MAddr, 0x10); break;
            case 0x67: BIT(IncsType.R8, 0x10, "A"); break;
            case 0x68: BIT(IncsType.R8, 0x20, "B"); break;
            case 0x69: BIT(IncsType.R8, 0x20, "C"); break;
            case 0x6A: BIT(IncsType.R8, 0x20, "D"); break;
            case 0x6B: BIT(IncsType.R8, 0x20, "E"); break;
            case 0x6C: BIT(IncsType.R8, 0x20, "H"); break;
            case 0x6D: BIT(IncsType.R8, 0x20, "L"); break;
            case 0x6E: BIT(IncsType.MAddr, 0x20); break;
            case 0x6F: BIT(IncsType.R8, 0x20, "A"); break;

            // 0x70 ~ 0x7F
            case 0x70: BIT(IncsType.R8, 0x40, "B"); break;
            case 0x71: BIT(IncsType.R8, 0x40, "C"); break;
            case 0x72: BIT(IncsType.R8, 0x40, "D"); break;
            case 0x73: BIT(IncsType.R8, 0x40, "E"); break;
            case 0x74: BIT(IncsType.R8, 0x40, "H"); break;
            case 0x75: BIT(IncsType.R8, 0x40, "L"); break;
            case 0x76: BIT(IncsType.MAddr, 0x40); break;
            case 0x77: BIT(IncsType.R8, 0x40, "A"); break;
            case 0x78: BIT(IncsType.R8, 0x80, "B"); break;
            case 0x79: BIT(IncsType.R8, 0x80, "C"); break;
            case 0x7A: BIT(IncsType.R8, 0x80, "D"); break;
            case 0x7B: BIT(IncsType.R8, 0x80, "E"); break;
            case 0x7C: BIT(IncsType.R8, 0x80, "H"); break;
            case 0x7D: BIT(IncsType.R8, 0x80, "L"); break;
            case 0x7E: BIT(IncsType.MAddr, 0x80); break;
            case 0x7F: BIT(IncsType.R8, 0x80, "A"); break;

            // 0x80 ~ 0x8F
            case 0x80: RES(IncsType.R8, 0x01, "B"); break;
            case 0x81: RES(IncsType.R8, 0x01, "C"); break;
            case 0x82: RES(IncsType.R8, 0x01, "D"); break;
            case 0x83: RES(IncsType.R8, 0x01, "E"); break;
            case 0x84: RES(IncsType.R8, 0x01, "H"); break;
            case 0x85: RES(IncsType.R8, 0x01, "L"); break;
            case 0x86: RES(IncsType.MAddr, 0x01); break;
            case 0x87: RES(IncsType.R8, 0x01, "A"); break;
            case 0x88: RES(IncsType.R8, 0x02, "B"); break;
            case 0x89: RES(IncsType.R8, 0x02, "C"); break;
            case 0x8A: RES(IncsType.R8, 0x02, "D"); break;
            case 0x8B: RES(IncsType.R8, 0x02, "E"); break;
            case 0x8C: RES(IncsType.R8, 0x02, "H"); break;
            case 0x8D: RES(IncsType.R8, 0x02, "L"); break;
            case 0x8E: RES(IncsType.MAddr, 0x02); break;
            case 0x8F: RES(IncsType.R8, 0x02, "A"); break;

            // 0x90 ~ 0x9F
            case 0x90: RES(IncsType.R8, 0x04, "B"); break;
            case 0x91: RES(IncsType.R8, 0x04, "C"); break;
            case 0x92: RES(IncsType.R8, 0x04, "D"); break;
            case 0x93: RES(IncsType.R8, 0x04, "E"); break;
            case 0x94: RES(IncsType.R8, 0x04, "H"); break;
            case 0x95: RES(IncsType.R8, 0x04, "L"); break;
            case 0x96: RES(IncsType.MAddr, 0x04); break;
            case 0x97: RES(IncsType.R8, 0x04, "A"); break;
            case 0x98: RES(IncsType.R8, 0x08, "B"); break;
            case 0x99: RES(IncsType.R8, 0x08, "C"); break;
            case 0x9A: RES(IncsType.R8, 0x08, "D"); break;
            case 0x9B: RES(IncsType.R8, 0x08, "E"); break;
            case 0x9C: RES(IncsType.R8, 0x08, "H"); break;
            case 0x9D: RES(IncsType.R8, 0x08, "L"); break;
            case 0x9E: RES(IncsType.MAddr, 0x08); break;
            case 0x9F: RES(IncsType.R8, 0x08, "A"); break;

            // 0xA0 ~ 0xAF
            case 0xA0: RES(IncsType.R8, 0x10, "B"); break;
            case 0xA1: RES(IncsType.R8, 0x10, "C"); break;
            case 0xA2: RES(IncsType.R8, 0x10, "D"); break;
            case 0xA3: RES(IncsType.R8, 0x10, "E"); break;
            case 0xA4: RES(IncsType.R8, 0x10, "H"); break;
            case 0xA5: RES(IncsType.R8, 0x10, "L"); break;
            case 0xA6: RES(IncsType.MAddr, 0x10); break;
            case 0xA7: RES(IncsType.R8, 0x10, "A"); break;
            case 0xA8: RES(IncsType.R8, 0x20, "B"); break;
            case 0xA9: RES(IncsType.R8, 0x20, "C"); break;
            case 0xAA: RES(IncsType.R8, 0x20, "D"); break;
            case 0xAB: RES(IncsType.R8, 0x20, "E"); break;
            case 0xAC: RES(IncsType.R8, 0x20, "H"); break;
            case 0xAD: RES(IncsType.R8, 0x20, "L"); break;
            case 0xAE: RES(IncsType.MAddr, 0x20); break;
            case 0xAF: RES(IncsType.R8, 0x20, "A"); break;

            // 0xB0 ~ 0xBF
            case 0xB0: RES(IncsType.R8, 0x40, "B"); break;
            case 0xB1: RES(IncsType.R8, 0x40, "C"); break;
            case 0xB2: RES(IncsType.R8, 0x40, "D"); break;
            case 0xB3: RES(IncsType.R8, 0x40, "E"); break;
            case 0xB4: RES(IncsType.R8, 0x40, "H"); break;
            case 0xB5: RES(IncsType.R8, 0x40, "L"); break;
            case 0xB6: RES(IncsType.MAddr, 0x40); break;
            case 0xB7: RES(IncsType.R8, 0x40, "A"); break;
            case 0xB8: RES(IncsType.R8, 0x80, "B"); break;
            case 0xB9: RES(IncsType.R8, 0x80, "C"); break;
            case 0xBA: RES(IncsType.R8, 0x80, "D"); break;
            case 0xBB: RES(IncsType.R8, 0x80, "E"); break;
            case 0xBC: RES(IncsType.R8, 0x80, "H"); break;
            case 0xBD: RES(IncsType.R8, 0x80, "L"); break;
            case 0xBE: RES(IncsType.MAddr, 0x80); break;
            case 0xBF: RES(IncsType.R8, 0x80, "A"); break;

            // 0xC0 ~ 0xCF
            case 0xC0: SET(IncsType.R8, 0x01, "B"); break;
            case 0xC1: SET(IncsType.R8, 0x01, "C"); break;
            case 0xC2: SET(IncsType.R8, 0x01, "D"); break;
            case 0xC3: SET(IncsType.R8, 0x01, "E"); break;
            case 0xC4: SET(IncsType.R8, 0x01, "H"); break;
            case 0xC5: SET(IncsType.R8, 0x01, "L"); break;
            case 0xC6: SET(IncsType.MAddr, 0x01); break;
            case 0xC7: SET(IncsType.R8, 0x01, "A"); break;
            case 0xC8: SET(IncsType.R8, 0x02, "B"); break;
            case 0xC9: SET(IncsType.R8, 0x02, "C"); break;
            case 0xCA: SET(IncsType.R8, 0x02, "D"); break;
            case 0xCB: SET(IncsType.R8, 0x02, "E"); break;
            case 0xCC: SET(IncsType.R8, 0x02, "H"); break;
            case 0xCD: SET(IncsType.R8, 0x02, "L"); break;
            case 0xCE: SET(IncsType.MAddr, 0x02); break;
            case 0xCF: SET(IncsType.R8, 0x02, "A"); break;

            // 0xD0 ~ 0xDF
            case 0xD0: SET(IncsType.R8, 0x04, "B"); break;
            case 0xD1: SET(IncsType.R8, 0x04, "C"); break;
            case 0xD2: SET(IncsType.R8, 0x04, "D"); break;
            case 0xD3: SET(IncsType.R8, 0x04, "E"); break;
            case 0xD4: SET(IncsType.R8, 0x04, "H"); break;
            case 0xD5: SET(IncsType.R8, 0x04, "L"); break;
            case 0xD6: SET(IncsType.MAddr, 0x04); break;
            case 0xD7: SET(IncsType.R8, 0x04, "A"); break;
            case 0xD8: SET(IncsType.R8, 0x08, "B"); break;
            case 0xD9: SET(IncsType.R8, 0x08, "C"); break;
            case 0xDA: SET(IncsType.R8, 0x08, "D"); break;
            case 0xDB: SET(IncsType.R8, 0x08, "E"); break;
            case 0xDC: SET(IncsType.R8, 0x08, "H"); break;
            case 0xDD: SET(IncsType.R8, 0x08, "L"); break;
            case 0xDE: SET(IncsType.MAddr, 0x08); break;
            case 0xDF: SET(IncsType.R8, 0x08, "A"); break;

            // 0xE0 ~ 0xEF
            case 0xE0: SET(IncsType.R8, 0x10, "B"); break;
            case 0xE1: SET(IncsType.R8, 0x10, "C"); break;
            case 0xE2: SET(IncsType.R8, 0x10, "D"); break;
            case 0xE3: SET(IncsType.R8, 0x10, "E"); break;
            case 0xE4: SET(IncsType.R8, 0x10, "H"); break;
            case 0xE5: SET(IncsType.R8, 0x10, "L"); break;
            case 0xE6: SET(IncsType.MAddr, 0x10); break;
            case 0xE7: SET(IncsType.R8, 0x10, "A"); break;
            case 0xE8: SET(IncsType.R8, 0x20, "B"); break;
            case 0xE9: SET(IncsType.R8, 0x20, "C"); break;
            case 0xEA: SET(IncsType.R8, 0x20, "D"); break;
            case 0xEB: SET(IncsType.R8, 0x20, "E"); break;
            case 0xEC: SET(IncsType.R8, 0x20, "H"); break;
            case 0xED: SET(IncsType.R8, 0x20, "L"); break;
            case 0xEE: SET(IncsType.MAddr, 0x20); break;
            case 0xEF: SET(IncsType.R8, 0x20, "A"); break;

            // 0xF0 ~ 0xFF
            case 0xF0: SET(IncsType.R8, 0x40, "B"); break;
            case 0xF1: SET(IncsType.R8, 0x40, "C"); break;
            case 0xF2: SET(IncsType.R8, 0x40, "D"); break;
            case 0xF3: SET(IncsType.R8, 0x40, "E"); break;
            case 0xF4: SET(IncsType.R8, 0x40, "H"); break;
            case 0xF5: SET(IncsType.R8, 0x40, "L"); break;
            case 0xF6: SET(IncsType.MAddr, 0x40); break;
            case 0xF7: SET(IncsType.R8, 0x40, "A"); break;
            case 0xF8: SET(IncsType.R8, 0x80, "B"); break;
            case 0xF9: SET(IncsType.R8, 0x80, "C"); break;
            case 0xFA: SET(IncsType.R8, 0x80, "D"); break;
            case 0xFB: SET(IncsType.R8, 0x80, "E"); break;
            case 0xFC: SET(IncsType.R8, 0x80, "H"); break;
            case 0xFD: SET(IncsType.R8, 0x80, "L"); break;
            case 0xFE: SET(IncsType.MAddr, 0x80); break;
            case 0xFF: SET(IncsType.R8, 0x80, "A"); break;

        }
    }
    
    
    
    private void SET(IncsType incsType, u8 bit, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            switch (data1)
            {
                case "B": _regs.B = (u8) (_regs.B | bit); break;
                case "C": _regs.C = (u8) (_regs.C | bit); break;
                case "D": _regs.D = (u8) (_regs.D | bit); break;
                case "E": _regs.E = (u8) (_regs.E | bit); break;
                case "H": _regs.H = (u8) (_regs.H | bit); break;
                case "L": _regs.L = (u8) (_regs.L | bit); break;
                case "A": _regs.A = (u8) (_regs.A | bit); break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            _mmu.Write(_regs.HL, (u8) (_mmu.Read(_regs.HL) | bit));
            _cycles += 16;
        }
    }
    private void RES(IncsType incsType, u8 bit, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            switch (data1)
            {
                case "B": _regs.B = (u8) (_regs.B & ~bit); break;
                case "C": _regs.C = (u8) (_regs.C & ~bit); break;
                case "D": _regs.D = (u8) (_regs.D & ~bit); break;
                case "E": _regs.E = (u8) (_regs.E & ~bit); break;
                case "H": _regs.H = (u8) (_regs.H & ~bit); break;
                case "L": _regs.L = (u8) (_regs.L & ~bit); break;
                case "A": _regs.A = (u8) (_regs.A & ~bit); break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            _mmu.Write(_regs.HL, (u8) (_mmu.Read(_regs.HL) & ~bit));
            _cycles += 16;
        }
    }
    private void BIT(IncsType incsType, u8 bit, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.SetFlag(Registers.Flag.Z, (value & bit) == 0);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, true);

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlag(Registers.Flag.Z, (value & bit) == 0);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, true);

            _cycles += 12;
        }
    }
    private void SRL(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            u8 result = (u8) (value >> 1);

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);

            switch (data1)
            {
                case "B": _regs.B = result; break;
                case "C": _regs.C = result; break;
                case "D": _regs.D = result; break;
                case "E": _regs.E = result; break;
                case "H": _regs.H = result; break;
                case "L": _regs.L = result; break;
                case "A": _regs.A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            u8 result = (u8) (value >> 1);
            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);
            _mmu.Write(_regs.HL, result);

            _cycles += 16;
        }
    }
    private void SWAP(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            u8 result = (u8) (((value & 0xF0) >> 4) | ((value & 0x0F) << 4));

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);

            switch (data1)
            {
                case "B": _regs.B = result; break;
                case "C": _regs.C = result; break;
                case "D": _regs.D = result; break;
                case "E": _regs.E = result; break;
                case "H": _regs.H = result; break;
                case "L": _regs.L = result; break;
                case "A": _regs.A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            u8 result = (u8) (((value & 0xF0) >> 4) | ((value & 0x0F) << 4));
            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);
            _mmu.Write(_regs.HL, result);

            _cycles += 16;
        }
    }

    private void SRA(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            u8 result = (u8) ((value >> 1) | (value & 0x80));

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);

            switch (data1)
            {
                case "B": _regs.B = result; break;
                case "C": _regs.C = result; break;
                case "D": _regs.D = result; break;
                case "E": _regs.E = result; break;
                case "H": _regs.H = result; break;
                case "L": _regs.L = result; break;
                case "A": _regs.A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            u8 result = (u8) ((value >> 1) | (value & 0x80));
            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);
            _mmu.Write(_regs.HL, result);

            _cycles += 16;
        }
    }

    private void SLA(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            u8 result = (u8) (value << 1);

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x80) != 0);

            switch (data1)
            {
                case "B": _regs.B = result; break;
                case "C": _regs.C = result; break;
                case "D": _regs.D = result; break;
                case "E": _regs.E = result; break;
                case "H": _regs.H = result; break;
                case "L": _regs.L = result; break;
                case "A": _regs.A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ((u8) (value << 1));
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x80) != 0);
            _mmu.Write(_regs.HL, (u8) (value << 1));

            _cycles += 16;
        }
    }

    private void RR(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            bool oldC = _regs.GetFlag(Registers.Flag.C);
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            u8 result = (u8) ((value >> 1) | (oldC ? 0x80 : 0));

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);

            switch (data1)
            {
                case "B": _regs.B = result; break;
                case "C": _regs.C = result; break;
                case "D": _regs.D = result; break;
                case "E": _regs.E = result; break;
                case "H": _regs.H = result; break;
                case "L": _regs.L = result; break;
                case "A": _regs.A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            bool oldC = _regs.GetFlag(Registers.Flag.C);
            u8 value = _mmu.Read(_regs.HL);
            u8 result = (u8) ((value >> 1) | (oldC ? 0x80 : 0));

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);
            _mmu.Write(_regs.HL, result);

            _cycles += 16;
        }
    } 

    private void RL(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            bool oldC = _regs.GetFlag(Registers.Flag.C);
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            u8 result = (u8) ((value << 1) | (oldC ? 1 : 0));

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x80) != 0);

            switch (data1)
            {
                case "B": _regs.B = result; break;
                case "C": _regs.C = result; break;
                case "D": _regs.D = result; break;
                case "E": _regs.E = result; break;
                case "H": _regs.H = result; break;
                case "L": _regs.L = result; break;
                case "A": _regs.A = result; break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            bool oldC = _regs.GetFlag(Registers.Flag.C);
            u8 value = _mmu.Read(_regs.HL);
            u8 result = (u8) ((value << 1) | (oldC ? 1 : 0));

            _regs.SetFlagZ(result);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x80) != 0);
            _mmu.Write(_regs.HL, result);

            _cycles += 16;
        }
    }

    private void RRC(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.SetFlagZ((u8) ((value >> 1) | (value << 7)));
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);

            switch (data1)
            {
                case "B": _regs.B = (u8) ((value >> 1) | (value << 7)); break;
                case "C": _regs.C = (u8) ((value >> 1) | (value << 7)); break;
                case "D": _regs.D = (u8) ((value >> 1) | (value << 7)); break;
                case "E": _regs.E = (u8) ((value >> 1) | (value << 7)); break;
                case "H": _regs.H = (u8) ((value >> 1) | (value << 7)); break;
                case "L": _regs.L = (u8) ((value >> 1) | (value << 7)); break;
                case "A": _regs.A = (u8) ((value >> 1) | (value << 7)); break;
            }

            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ((u8) ((value >> 1) | (value << 7)));
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x01) != 0);
            _mmu.Write(_regs.HL, (u8) ((value >> 1) | (value << 7)));

            _cycles += 16;
        }
    }

    private void RLC(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.SetFlagZ((u8) ((value << 1) | (value >> 7)));
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x80) != 0);

            switch (data1)
            {
                case "B": _regs.B = (u8) ((value << 1) | (value >> 7)); break;
                case "C": _regs.C = (u8) ((value << 1) | (value >> 7)); break;
                case "D": _regs.D = (u8) ((value << 1) | (value >> 7)); break;
                case "E": _regs.E = (u8) ((value << 1) | (value >> 7)); break;
                case "H": _regs.H = (u8) ((value << 1) | (value >> 7)); break;
                case "L": _regs.L = (u8) ((value << 1) | (value >> 7)); break;
                case "A": _regs.A = (u8) ((value << 1) | (value >> 7)); break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ((u8) ((value << 1) | (value >> 7)));
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, (value & 0x80) != 0);
            _mmu.Write(_regs.HL, (u8) ((value << 1) | (value >> 7)));

            _cycles += 16;
        }
    }
    private void NOP() { _cycles += 4; }

    private void LD(IncsType incsType, string data1 = "", string data2 = "")
    {
        if (incsType == IncsType.R16_D16)
        {
            switch (data1)
            {
                case "BC": _regs.BC = _mmu.ReadROM16(_regs.PC); break;
                case "DE": _regs.DE = _mmu.ReadROM16(_regs.PC); break;
                case "HL": _regs.HL = _mmu.ReadROM16(_regs.PC); break;
                case "SP": _regs.SP = _mmu.ReadROM16(_regs.PC); break;
            }
            _regs.PC += 2; 
            _cycles += 12;
        }
        else if (incsType == IncsType.MAddr_R8)
        {
            u16 address = 0;
            u8 value = 0;
            switch (data1)
            {
                case "BC": address = _regs.BC; break;
                case "DE": address = _regs.DE; break;
                case "HL": address = _regs.HL; break;
                case "HL+": address = _regs.HL; _regs.HL ++; break;
                case "HL-": address = _regs.HL; _regs.HL --; break;
            }

            switch (data2)
            {
                case "A": value = _regs.A; break;
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
            }

            _mmu.Write(address, value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            switch (data1)
            {
                case "B": _regs.B = _mmu.Read(_regs.PC ++); break;
                case "D": _regs.D = _mmu.Read(_regs.PC ++); break;
                case "H": _regs.H = _mmu.Read(_regs.PC ++); break;
                case "C": _regs.C = _mmu.Read(_regs.PC ++); break;
                case "E": _regs.E = _mmu.Read(_regs.PC ++); break;
                case "L": _regs.L = _mmu.Read(_regs.PC ++); break;
                case "A": _regs.A = _mmu.Read(_regs.PC ++); break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.A16_R16)
        {
            _mmu.WriteROM16(_mmu.ReadROM16(_regs.PC), _regs.SP);
            _regs.PC += 2;
            _cycles += 20;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = 0;
            switch (data2)
            {
                case "BC": value = _mmu.Read(_regs.BC); break;
                case "DE": value = _mmu.Read(_regs.DE); break;
                case "HL+": value = _mmu.Read(_regs.HL ++); break;
                case "HL-": value = _mmu.Read(_regs.HL --); break;
                case "HL": value = _mmu.Read(_regs.HL); break;
            }
            
            switch (data1)
            {
                case "A": _regs.A = value; break;
                case "B": _regs.B = value; break;
                case "C": _regs.C = value; break;
                case "D": _regs.D = value; break;
                case "E": _regs.E = value; break;
                case "H": _regs.H = value; break;
                case "L": _regs.L = value; break;
            }
            
            _cycles += 8;
        }
        else if (incsType == IncsType.MAddr_D8)
        {
            _mmu.Write(_regs.HL, _mmu.Read(_regs.PC ++));
            _cycles += 12;
        }
        else if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data2)
            {
                case "A": value = _regs.A; break;
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "L": value = _regs.L; break;
            }

            switch (data1)
            {
                case "A": _regs.A = value; break;
                case "B": _regs.B = value; break;
                case "C": _regs.C = value; break;
                case "D": _regs.D = value; break;
                case "E": _regs.E = value; break;
                case "H": _regs.H = value; break;
                case "L": _regs.L = value; break;
            }

            _cycles += 4;
        }
        else if (incsType == IncsType.A8_R8)
        {
            u16 value1 = (u16) (0xFF00 + _mmu.Read(_regs.PC));
            u8 value2 = _regs.A;
            _mmu.Write((u16) (0xFF00 + _mmu.Read(_regs.PC ++)), _regs.A);
            _cycles += 12;
        }
        else if (incsType == IncsType.R8_A8)
        {
            _regs.A = _mmu.Read((u16) (0xFF00 + _mmu.Read(_regs.PC ++)));
            _cycles += 12;
        }
        else if (incsType == IncsType.MAddr8_R8)
        {
            _mmu.Write((u16) (0xFF00 + _regs.C), _regs.A);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_MAddr8)
        {
            _regs.A = _mmu.Read((u16) (0xFF00 + _regs.C));
            _cycles += 8;
        }
        else if (incsType == IncsType.R16_R16S8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.SetFlag(Registers.Flag.Z, false);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH((u8) _regs.SP, value);
            _regs.SetFlagC((u8) _regs.SP + value);
            _regs.HL = (u16) (_regs.SP + (sbyte) value);
            _cycles += 12;
        }
        else if (incsType == IncsType.R16_R16)
        {
            _regs.SP = _regs.HL;
            _cycles += 8;
        }
        else if (incsType == IncsType.A16_R8)
        {
            _mmu.Write(_mmu.ReadROM16(_regs.PC), _regs.A);
            _regs.PC += 2;
            _cycles += 16;
        }
        else if (incsType == IncsType.R8_A16)
        {
            _regs.A = _mmu.Read(_mmu.ReadROM16(_regs.PC));
            _regs.PC += 2;
            _cycles += 16;
        }
    }

    private void INC(IncsType incsType, string data)
    {
        if (incsType == IncsType.R16)
        {
            switch (data)
            {
                case "BC": _regs.BC += 1; break;
                case "DE": _regs.DE += 1; break;
                case "HL": _regs.HL += 1; break;
                case "SP": _regs.SP += 1; break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data)
            {
                case "B": value = _regs.B; _regs.B += 1; break;
                case "D": value = _regs.D; _regs.D += 1; break;
                case "H": value = _regs.H; _regs.H += 1; break;
                case "C": value = _regs.C; _regs.C += 1; break;
                case "E": value = _regs.E; _regs.E += 1; break;
                case "L": value = _regs.L; _regs.L += 1; break;
                case "A": value = _regs.A; _regs.A += 1; break;
            }

            // set flag
            _regs.SetFlagZ(value + 1);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH(value, 1);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            if (data == "HL")
            {
                u8 value = _mmu.Read(_regs.HL);
                _mmu.Write(_regs.HL, (u8) (value + 1));
                // set flag
                _regs.SetFlagZ(value + 1);
                _regs.SetFlag(Registers.Flag.N, false);
                _regs.SetFlagH(value, 1);
                _cycles += 12;
            }
        }
    }

    private void DEC(IncsType incsType, string data)
    {
        if (incsType == IncsType.R16)
        {
            switch (data)
            {
                case "BC": _regs.BC -= 1; break;
                case "DE": _regs.DE -= 1; break;
                case "HL": _regs.HL -= 1; break;
                case "SP": _regs.SP -= 1; break;
            }
            _cycles += 8;
        }
        else if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data)
            {
                case "B": value = _regs.B; _regs.B -= 1; break;
                case "D": value = _regs.D; _regs.D -= 1; break;
                case "H": value = _regs.H; _regs.H -= 1; break;
                case "C": value = _regs.C; _regs.C -= 1; break;
                case "E": value = _regs.E; _regs.E -= 1; break;
                case "L": value = _regs.L; _regs.L -= 1; break;
                case "A": value = _regs.A; _regs.A -= 1; break;
            }

            // set flag
            _regs.SetFlagZ(value - 1);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(value, 1, Registers.FlagH.Sub);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            if (data == "HL")
            {
                u8 value = _mmu.Read(_regs.HL);
                _mmu.Write(_regs.HL, (u8) (value - 1));
                // set flag
                _regs.SetFlagZ(value - 1);
                _regs.SetFlag(Registers.Flag.N, true);
                _regs.SetFlagH(value, 1, Registers.FlagH.Sub);
                _cycles += 12;
            }
        }
    }
    private void RLCA()
    {
        _regs.F = 0;
        _regs.SetFlag(Registers.Flag.C, ((_regs.A & 0x80) != 0));
        _regs.A = (u8) ((_regs.A << 1) | (_regs.A >> 7));
        _cycles += 4;
    }
    private void ADD(IncsType incsType, string data1 = "", string data2 = "")
    {
        if (incsType == IncsType.R16_R16)
        {
            u16 value = 0;
            switch (data1)
            {
                case "BC": value = _regs.BC; break;
                case "DE": value = _regs.DE; break;
                case "HL": value = _regs.HL; break;
                case "SP": value = _regs.SP; break;
            }
            // set flag
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH(_regs.HL, value);
            _regs.SetFlag(Registers.Flag.C, (_regs.HL + value) >> 16 != 0);

            _regs.HL = (u16) (_regs.HL + value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }
            _regs.SetFlagZ(_regs.A + value);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH(_regs.A, value);
            _regs.SetFlagC(_regs.A + value);
            _regs.A = (u8) (_regs.A + value);
            _cycles += 4;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ(_regs.A + value);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH(_regs.A, value);
            _regs.SetFlagC(_regs.A + value);
            _regs.A = (u8) (_regs.A + value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.SetFlagZ(_regs.A + value);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH(_regs.A, value);
            _regs.SetFlagC(_regs.A + value);
            _regs.A = (u8) (_regs.A + value);
            _cycles += 8;
        }
        else if (incsType == IncsType.R16_S8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.SetFlag(Registers.Flag.Z, false);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlagH((u8) _regs.SP, value);
            _regs.SetFlagC((u8) _regs.SP + value);
            _regs.SP = (u16) (_regs.SP + (sbyte) value);
            _cycles += 16;
        }
    }
    private void RRCA()
    {
        _regs.F = 0;
        _regs.SetFlag(Registers.Flag.C, ((_regs.A & 0x01) != 0));
        _regs.A = (u8) ((_regs.A >> 1) | (_regs.A << 7));

        _cycles += 4;
    }
    private void STOP() {
        throw new NotImplementedException();
    }

    private void RLA()
    {
        bool oldC = _regs.GetFlag(Registers.Flag.C);
        _regs.F = 0;
        _regs.SetFlag(Registers.Flag.C, ((_regs.A & 0x80) != 0));
        _regs.A = (u8) ((_regs.A << 1) | (oldC ? 1 : 0));
    }

    private void JR(bool state)
    {
        if (state)
        {
            sbyte sb = (sbyte) _mmu.Read(_regs.PC);
            _regs.PC = (u16) (_regs.PC + sb);
            _regs.PC += 1;
            _cycles += 12;
        }
        else
        {
            _regs.PC += 1;
            _cycles += 8;
        }
    }

    private void RRA()
    {
        bool oldC = _regs.GetFlag(Registers.Flag.C);
        _regs.F = 0;
        _regs.SetFlag(Registers.Flag.C, ((_regs.A & 0x01) != 0));
        _regs.A = (u8) ((_regs.A >> 1) | (oldC ? 0x80 : 0));
        _cycles += 4;
    }

    private void DAA()
    {
        if (_regs.GetFlag(Registers.Flag.N))
        {
            if (_regs.GetFlag(Registers.Flag.C)) _regs.A -= 0x60;
            if (_regs.GetFlag(Registers.Flag.H)) _regs.A -= 0x06;
        }
        else
        {
            if (_regs.GetFlag(Registers.Flag.C) || (_regs.A > 0x99)) { _regs.A += 0x60; _regs.SetFlag(Registers.Flag.C, true); }
            if (_regs.GetFlag(Registers.Flag.H) || ((_regs.A) & 0x0F) > 0x09) _regs.A += 0x06;
        }
        _regs.SetFlagZ(_regs.A);
        _regs.SetFlag(Registers.Flag.H, false);
        _cycles += 4;
    }

    private void CPL()
    {
        _regs.A = (u8) (~_regs.A);
        _regs.SetFlag(Registers.Flag.N, true);
        _regs.SetFlag(Registers.Flag.H, true);
        _cycles += 4;
    }

    private void SCF()
    {
        _regs.SetFlag(Registers.Flag.C, true);
        _regs.SetFlag(Registers.Flag.N, false);
        _regs.SetFlag(Registers.Flag.H, false);
        _cycles += 4;
    }

    private void CCF()
    {
        _regs.SetFlag(Registers.Flag.C, !_regs.GetFlag(Registers.Flag.C));
        _regs.SetFlag(Registers.Flag.N, false);
        _regs.SetFlag(Registers.Flag.H, false);
        _cycles += 4;
    }

    private void HALT()
    {
        if (!_ime && ((_mmu.GetIE() & _mmu.IFRegister & 0x1F) == 0)) { _isHalted = true; _regs.PC --;}
        _cycles += 4;
    }

    private void ADC(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.SetFlagZ(_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.SetFlag(Registers.Flag.N, false);
            if (_regs.GetFlag(Registers.Flag.C)) { _regs.SetFlagH(_regs.A, value, Registers.FlagH.Carry); }
            else { _regs.SetFlagH(_regs.A, value); }
            _regs.SetFlagC(_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.A = (u8) (_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _cycles += 4;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ(_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.SetFlag(Registers.Flag.N, false);
            if (_regs.GetFlag(Registers.Flag.C)) { _regs.SetFlagH(_regs.A, value, Registers.FlagH.Carry); }
            else { _regs.SetFlagH(_regs.A, value); }
            _regs.SetFlagC(_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.A = (u8) (_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.SetFlagZ(_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.SetFlag(Registers.Flag.N, false);
            if (_regs.GetFlag(Registers.Flag.C)) { _regs.SetFlagH(_regs.A, value, Registers.FlagH.Carry); }
            else { _regs.SetFlagH(_regs.A, value); }
            _regs.SetFlagC(_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.A = (u8) (_regs.A + value + (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _cycles += 8;
        }
    }

    private void SUB(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }
            _regs.SetFlagZ(_regs.A - value);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub);
            _regs.SetFlagC(_regs.A - value);
            _regs.A = (u8) (_regs.A - value);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ(_regs.A - value);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub);
            _regs.SetFlagC(_regs.A - value);
            _regs.A = (u8) (_regs.A - value);
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.SetFlagZ(_regs.A - value);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub);
            _regs.SetFlagC(_regs.A - value);
            _regs.A = (u8) (_regs.A - value);
            _cycles += 8;
        }
    }

    private void SBC(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8_R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.SetFlagZ(_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.SetFlag(Registers.Flag.N, true);
            if (_regs.GetFlag(Registers.Flag.C)) { _regs.SetFlagH(_regs.A, value, Registers.FlagH.SubCarry); }
            else { _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub); }
            _regs.SetFlagC(_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.A = (u8) (_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _cycles += 4;
        }
        else if (incsType == IncsType.R8_MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);

            _regs.SetFlagZ(_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.SetFlag(Registers.Flag.N, true);
            if (_regs.GetFlag(Registers.Flag.C)) { _regs.SetFlagH(_regs.A, value, Registers.FlagH.SubCarry); }
            else { _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub); }
            _regs.SetFlagC(_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.A = (u8) (_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _cycles += 8;
        }
        else if (incsType == IncsType.R8_D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);

            _regs.SetFlagZ(_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.SetFlag(Registers.Flag.N, true);
            if (_regs.GetFlag(Registers.Flag.C)) { _regs.SetFlagH(_regs.A, value, Registers.FlagH.SubCarry); }
            else { _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub); }
            _regs.SetFlagC(_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _regs.A = (u8) (_regs.A - value - (_regs.GetFlag(Registers.Flag.C) ? 1 : 0));
            _cycles += 8;
        }
    }

    private void AND(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.A = (u8) (_regs.A & value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, true);
            _regs.SetFlag(Registers.Flag.C, false);

            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);

            _regs.A = (u8) (_regs.A & value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, true);
            _regs.SetFlag(Registers.Flag.C, false);

            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.A = (u8) (_regs.A & value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, true);
            _regs.SetFlag(Registers.Flag.C, false);

            _cycles += 8;
        }
    }

    private void XOR(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.A = (u8) (_regs.A ^ value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);

            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);

            _regs.A = (u8) (_regs.A ^ value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(_regs.PC);

            _regs.A = (u8) (_regs.A ^ value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);
            _cycles += 8;
        }
    }

    private void OR(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }

            _regs.A = (u8) (_regs.A | value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);

            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);

            _regs.A = (u8) (_regs.A | value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.A = (u8) (_regs.A | value);
            _regs.SetFlagZ(_regs.A);
            _regs.SetFlag(Registers.Flag.N, false);
            _regs.SetFlag(Registers.Flag.H, false);
            _regs.SetFlag(Registers.Flag.C, false);
            _cycles += 8;
        }
    }

    private void CP(IncsType incsType, string data1 = "")
    {
        if (incsType == IncsType.R8)
        {
            u8 value = 0;
            switch (data1)
            {
                case "B": value = _regs.B; break;
                case "C": value = _regs.C; break;
                case "D": value = _regs.D; break;
                case "E": value = _regs.E; break;
                case "H": value = _regs.H; break;
                case "L": value = _regs.L; break;
                case "A": value = _regs.A; break;
            }
            _regs.SetFlagZ(_regs.A - value);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub);
            _regs.SetFlagC(_regs.A - value);
            _cycles += 4;
        }
        else if (incsType == IncsType.MAddr)
        {
            u8 value = _mmu.Read(_regs.HL);
            _regs.SetFlagZ(_regs.A - value);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub);
            _regs.SetFlagC(_regs.A - value);
            _cycles += 8;
        }
        else if (incsType == IncsType.D8)
        {
            u8 value = _mmu.Read(_regs.PC ++);
            _regs.SetFlagZ(_regs.A - value);
            _regs.SetFlag(Registers.Flag.N, true);
            _regs.SetFlagH(_regs.A, value, Registers.FlagH.Sub);
            _regs.SetFlagC(_regs.A - value);
            _cycles += 8;
        }
    }

    private u16 POP(string data1 = "")
    {
        u16 value = _mmu.ReadROM16(_regs.SP);
        _regs.SP += 2;

        switch (data1)
        {
            case "BC": _regs.BC = value; break;
            case "DE": _regs.DE = value; break;
            case "HL": _regs.HL = value; break;
            case "AF": _regs.AF = value; break;
        }
        _cycles += 12;
        return value;
    }

    private void PUSH(string data1, IncsType incsType = IncsType.R16, u16 number = 0)
    {
        if (incsType == IncsType.R16)
        {
            u16 value = 0;
            switch (data1)
            {
                case "BC": value = _regs.BC; break;
                case "DE": value = _regs.DE; break;
                case "HL": value = _regs.HL; break;
                case "AF": value = _regs.AF; break;
            }

            _regs.SP -= 2;
            _mmu.WriteROM16(_regs.SP, value);
            _cycles += 16;
        }
        else if (incsType == IncsType.A16)
        {
            _regs.SP -= 2;
            _mmu.WriteROM16(_regs.SP, number);
            _cycles += 16;
        }
        else if (incsType == IncsType.NO_CYCLE)
        {
            _regs.SP -= 2;
            _mmu.WriteROM16(_regs.SP, number);
        }
    }
    
    private void RET(bool flag)
    {
        if (flag) { _regs.PC = POP(); }
        _cycles += 8;
    }

    private void JP(bool flag, IncsType incsType = IncsType.A16)
    {
        if (incsType == IncsType.A16)
        {
            if (flag) { _regs.PC = _mmu.ReadROM16(_regs.PC); _cycles += 16; }
            else { _regs.PC += 2; _cycles += 12; }
        }
        else if (incsType == IncsType.MAddr)
        {
            _regs.PC = _regs.HL;
            _cycles += 4;
        }
    }

    private void CALL(bool flag)
    {
        if (flag)
        {
            PUSH("", IncsType.A16, (u16) (_regs.PC + 2));
            _cycles -= 16; // PUSH Cycle
            _regs.PC = _mmu.ReadROM16(_regs.PC);
            _cycles += 24;
        }
        else
        {
            _regs.PC += 2;
            _cycles += 12;
        }
    }

    private void DI()
    {
        _ime = false;
        _cycles += 4;
    }

    private void RST(u8 value)
    {
        PUSH("", IncsType.A16, _regs.PC);
        _regs.PC = value;
        _cycles -= 16; // PUSH Cycle
        _cycles += 16;
    }

    private void EI()
    {
        _enablingIME = true;
        _cycles += 4;
    }

    private void RETI()
    {
        _regs.PC = POP();
        _cycles -= 12; // POP Cycle
        _cycles += 16;
        _ime = true;
    }

}