/*
GB Manual : 
[$FF05] = $00 ; TIMA
[$FF06] = $00 ; TMA
[$FF07] = $00 ; TAC

This interval timer interrupts 4096 times per second
    ld a,-1
    ld ($FF06),a ;Set TMA to divide clock by 1
    ld a,4
    ld ($FF07),a ;Set clock to 4096 Hertz
    ;This interval timer interrupts 65536 times per second
    ld a,-4
    ld ($FF06),a ;Set TMA to divide clock by 4
    ld a,5
    ld ($FF07),a ;Set clock to 262144 Hertz
----
DIV 寄存器更新的頻率
    DMG : 每 256 個 CPU 時鐘週期，DIV 寄存器增量一次
    CGB : 每 512 個 CPU 時鐘週期，DIV 寄存器增量一次

硬體寄存器 TAC（Timer Control）  TIMA 計時器的四種運作頻率
    1024 週期（4096 Hz）
    16 週期（262144 Hz）
    64 週期（65536 Hz）
    256 週期（16384 Hz）
*/
using u8 = System.Byte;
public class Timer
{
    private int DIV = 0;
    private int TIMA = 0;

    public void UpdateDIV(ref MMU mmu, int cycles)
    {
        DIV += cycles;
        while (DIV >= 256)
        {
            mmu.SetDIV((u8) (mmu.GetDIV() + 1));
            DIV -= 256;
        }
    }

    public void UpdateTIMA(ref MMU mmu, int cycles)
    {
        if (mmu.GetTACState())
        {
            TIMA += cycles;
            while (TIMA >= mmu.GetTACFrequence())
            {
                mmu.SetTIMA((u8) (mmu.GetTIMA() + 1));
                TIMA -= mmu.GetTACFrequence();
            }

            if (mmu.GetTIMA() == 0xFF)
            {
                mmu.RequestInterrupt(2);
                mmu.SetTIMA(mmu.GetTMA());
            }
        }
    }
}