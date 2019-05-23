using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Enums
{
    public enum GeneticPitch
    {
        C3 = 1,
        D3 = 2,
        E3 = 3,
        F3 = 4,
        G3 = 5,
        A3 = 6,
        B3 = 7,
        C4 = 8,
        D4 = 9,
        E4 = 10,
        F4 = 11,
        G4 = 12,
        A4 = 13,
        B4 = 14,
        C5 = 15,
        D5 = 16,
        E5 = 17,
        F5 = 18,
        G5 = 19,
        A5 = 20,
        B5 = 21,
        C6 = 22,
        D6 = 23,
        E6 = 24,
        F6 = 25,
        G6 = 26,
        A6 = 27,
        B6 = 28
    }

    public static class GeneticPitchExtentionMethods
    {
        public static Pitch GeneticPitchToMidiPitch(this GeneticPitch geneticPitch, bool isSharp)
        {
            int resultPitch = 0;

            switch(((int)geneticPitch-1) % 7)
            {
                case 0: // C
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 48;
                    break;
                case 1: // D
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 50;
                    break;
                case 2: // E
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 52;
                    break;
                case 3: // F
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 53;
                    break;
                case 4: // G
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 55;
                    break;
                case 5: // A
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 57;
                    break;
                case 6: // B
                    resultPitch = (((int)geneticPitch - 1) / 7) * 12 + 59;
                    break;
            }
            
            if(isSharp == true)
            {
                resultPitch += 1;
            }

            return (Pitch)resultPitch;
        }
    }
}
