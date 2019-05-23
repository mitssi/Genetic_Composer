using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Enums
{
    public enum MusicalFont
    {
        // 구멍 있는 루트 노트
        NoteRootHole = (char)250,
        // 구멍 없는 까만 루트 노트
        NoteRootBlack = (char)246,
        
        // 기둥
        NotePillarUp = (char)92,
        NotePillarDown = (char)124,

        // 8분음표 꼬리
        Note8Down = (char)74,
        Note8Up = (char)106,

        // 16분음표 꼬리
        Note16Down = (char)75,
        Note16Up = (char)107,

        // 드럼 노트
        NoteDrum = (char)192,

        // 온음표
        Note1 = 'w',
        // 2분음표
        Note2 = 'h',
        // 4분음표
        Note4 = 'q',
        // 8분음표
        Note8 = 'e',
        // 16분음표
        Note16 = (char)218,

        // 온쉼표
        Rest1 = (char)238,
        // 4분쉼표
        Rest4 = (char)206,
        // 8분쉼표
        Rest8 = (char)228,
        // 16분쉼표
        Rest16 = (char)197,

        // 샵
        Sharp = '#',
        // 플랫
        Flat = 'b',

        // 높은음자리표
        Clef_G = '&',
        // 일반 Bar
        Bar_Normal = 'l',
        // 끝맻음 Bar
        Bar_End = '[',

        // 오선 음 벗어날 때 나오는 -
        OutLine = '-'
    }
}
