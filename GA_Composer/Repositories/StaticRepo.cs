using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Repositories
{
    public static class StaticRepo
    {
        public static ScoreRepository ScoreRepository = new ScoreRepository();

        public static ConfigRepository ConfigRepository = new ConfigRepository();

        public static TableRepository TableRepository = new TableRepository();

        public static GARepository GARepository = new GARepository();
    }
}
