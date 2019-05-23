using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Models
{
    public class Signal
    {
        public Channel Channel { get; set; }
    }

    public class NoteSignal : Signal
    {
        /// <summary>
        /// 노트가 On 인지 Off 인지
        /// </summary>
        public bool NoteOn { get; set; }

        public Pitch Pitch { get; set; }

        public int Velocity { get; set; }
    }

    public class ControlSignal : Signal
    {
        public Control Control { get; set; }

        public int Value { get; set; }
    }

    public class PlaySignal
    {
        /// <summary>
        /// 노트의 위치
        /// </summary>
        public int Offset { get; set; }

        public List<Signal> SignalList { get; set; }
    }
}
