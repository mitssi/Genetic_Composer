using GA_Composer.GeneticAlgorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Repositories
{
    public class GARepository
    {
        public IGA iGA = new GA(100, 0.1, 8);

        /// <summary>
        /// Generation 별로 애널리틱스를 다 가지고 있다
        /// </summary>
        public List<GA_Analytics> Analytics;

        public GARepository()
        {
            Init_Analytics();
        }

        public void Init_Analytics()
        {
            Analytics = new List<GA_Analytics>();
        }

        public void Add_Analytisc()
        {
            try
            {
                Analytics.Add(new GA_Analytics
                {
                    Generation = Analytics.Count()+1,
                });
            }
            catch(Exception e)
            {
                throw new Exception("Insert Analytics : " + e.ToString());
            }
        }

        /// <summary>
        /// 애널리틱스의 요소를 업데이트한다
        /// </summary>
        /// <param name="generation">업데이트 할 요소의 세대수</param>
        /// <param name="GAEnum">어떤 부분을 업데이트 할지</param>
        /// <param name="best">Best</param>
        /// <param name="average">Average</param>
        /// <param name="worst">Worst</param>
        public void Modify_Analytisc(int generation, GA_AnalEnum GAEnum, double[][] best, double[][] average, double[][] worst)
        {
            try
            {
                Analytics[generation - 1].ModifyAnalytics(GAEnum, best, average, worst);
            }
            catch (Exception e)
            {
                throw new Exception("Insert Analytics : " + e.ToString());
            }
        }
    }
    
    public enum GA_AnalEnum
    {
        Fitness = 0,
        OneNoteTable = 1,
        TwoNoteTable = 2,
        NumOfNotes = 3,
        HighLowDifference = 4,
        Inflection = 5,
        PitchDifference = 6,
        NumOfLength = 7,
        OffsetOfStartTime = 8,
        JazzRhythmScore = 9
    }

    /// <summary>
    /// 애널리틱스 다 모아놓은것
    /// double[GA_AnalEnum][BarNumber]
    /// </summary>
    public class GA_Analytics
    {
        public int Generation;
        public double[][] Best;
        public double[][] Average;
        public double[][] Worst;
        
        public GA_Analytics()
        {
            Best = new double[Enum.GetNames(typeof(GA_AnalEnum)).Length][];
            Average = new double[Enum.GetNames(typeof(GA_AnalEnum)).Length][];
            Worst = new double[Enum.GetNames(typeof(GA_AnalEnum)).Length][];
        }

        public void ModifyAnalytics(GA_AnalEnum GAEnum, double[][] best, double[][] average, double[][] worst)
        {
            Best = best;
            Average = average;
            Worst = worst;
        }
    }
}
