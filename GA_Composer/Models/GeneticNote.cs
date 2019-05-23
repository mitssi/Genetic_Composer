using GA_Composer.Enums;
using Midi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Models
{
    public class GeneticGraphicNote : GeneticNote
    {
        /// <summary>
        /// 클릭 이벤트 핸들러의 좌측 상단 경계점
        /// </summary>
        public Point Lefthigh { get; set; }

        /// <summary>
        /// 클릭 이벤트 핸들러의 우측 상단 경계점
        /// </summary>
        public Point Righthigh { get; set; }

        /// <summary>
        /// 클릭 이벤트 핸들러의 좌측 하단 경계점
        /// </summary>
        public Point Leftbottom { get; set;  }

        /// <summary>
        /// 클릭 이벤트 핸들러의 우측 하단 경계점
        /// </summary>
        public Point Rightbottom { get; set; }

        /// <summary>
        /// 현재 선택되어 있는 음표인지
        /// </summary>
        public bool isSeleted { get; set; } = false;

        /// <summary>
        /// 음표가 있는 위치
        /// </summary>
        public int Offset { get; set; }
    }

    public class GeneticNote
    {
        public GeneticPitch Pitch { get; set; }

        public int Duration { get; set; }

        /// <summary>
        /// 음표인지 쉼표인지
        /// </summary>
        public bool isRest { get; set; }

        public bool isSharp { get; set; }

        public Pitch ToPitch
        {
            get
            {
                return Pitch.GeneticPitchToMidiPitch(isSharp);
            }
        }

    }
}
