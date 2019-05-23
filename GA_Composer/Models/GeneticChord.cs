using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Models
{
    public class GeneticChord
    {
        /// <summary>
        /// 어떤 코드인지
        /// </summary>
        public Chord Chord { get; set; }

        /// <summary>
        /// 어떤 위치에 있는지
        /// </summary>
        /// <remarks>
        /// 위치는 960을 4분의 1박자로 본다
        /// </remarks>
        public int OffSet { get; set; }
    }
}
