using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Models
{
    /// <summary>
    /// 마디
    /// </summary>
    public class GeneticBar
    {
        #region Properties

        public List<GeneticGraphicNote> Notes { get; set; } = new List<GeneticGraphicNote>();

        #endregion

        #region Constructor

        public GeneticBar()
        {

        }
        #endregion

    }
}
