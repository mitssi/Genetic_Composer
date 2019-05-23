using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.GeneticAlgorithm
{
    public interface IGA
    {
        int Population { get; set; }

        int NumOfBar { get; set; }

        double MutationPercentage { get; set; }

        Chromosome[][] ChromosomeList { get; set; }

        void InitGA(int numOfBar);

        void NextGeneration();

        void NextGenerationBar(int barNumber);
    }
}
