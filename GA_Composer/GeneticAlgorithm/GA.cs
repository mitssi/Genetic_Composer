using GA_Composer.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GA_Composer.GeneticAlgorithm
{
    public class GA : IGA
    {
        /// <summary>
        /// Population of GA
        /// </summary>
        public int Population { get; set; }

        /// <summary>
        /// Crossover 후에 변이될 확률
        /// </summary>
        public double MutationPercentage { get; set; }

        /// <summary>
        /// Chromosome 의 List. Population 의 개수만큼 있다
        /// </summary>
        public Chromosome[][] ChromosomeList { get; set; }

        /// <summary>
        /// 마디의 개수
        /// </summary>
        public int NumOfBar { get; set; }

        /// <summary>
        /// Crossover 에 사용되는 Edit Distance
        /// 일단 최대한 크게 만들어놓는다.
        /// </summary>
        private double[][] EditDistanceTable;
        
        private class NoteScore
        {
            public GeneNote note;
            public bool selected;
        }

        /// <summary>
        /// Edit distance table 을 완료한다음에 역추적한 결과를 넣는 테이블
        /// 새로운 Chromosome 을 만드는데에는 이 테이블을 참고로 한다.
        /// 이 테이블의 크기는 아무리 커도 염색체의 최대크기 x 2 를 넘지 않는다.
        /// 따라서 생성자에서 크기를 최대크기 x 2 로 잡는다.
        /// 일일히 이 공간을 생성해주기에는 많은 시간적, 공간적인 불리함이 뒤따른다.
        /// </summary>
        private List<NoteScore> ScoreTable;

        public GA(int population, double mutationPercentage, int numOfBar)
        {
            NumOfBar = numOfBar;
            Population = population;
            MutationPercentage = mutationPercentage;

            // Edit Distance Table 초기화
            EditDistanceTable = new double[StaticRepo.ConfigRepository.MaxNumOfNote+1][];

            for(int i=0; i<= StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            {
                EditDistanceTable[i] = new double[StaticRepo.ConfigRepository.MaxNumOfNote+1];
            }
        }

        public void InitGA(int numOfBar)
        {
            StaticRepo.ConfigRepository.Generation = 1;
            NumOfBar = numOfBar;
            ChromosomeList = new Chromosome[numOfBar][];
            StaticRepo.TableRepository.ConvertChordTable();

            for (int i = 0; i < numOfBar; i++)
            {
                ChromosomeList[i] = new Chromosome[Population];
                for (int j = 0; j < Population; j++)
                {
                    ChromosomeList[i][j] = new Chromosome(i);
                    ChromosomeList[i][j].InitRandomGene();
                }
            }

            checkList();
            UpdateAnalytics();
            StaticRepo.ScoreRepository.MakeNotesUsingGA();
            StaticRepo.ConfigRepository.BestFitness = 0;
            StaticRepo.ConfigRepository.AverageFitness = 0;
            

            for (int i = 0; i < NumOfBar; i++)
            {
                StaticRepo.ConfigRepository.BestFitness += StaticRepo.GARepository.Analytics[StaticRepo.ConfigRepository.Generation - 1].Best[0][i];
                StaticRepo.ConfigRepository.AverageFitness += StaticRepo.GARepository.Analytics[StaticRepo.ConfigRepository.Generation - 1].Average[0][i];
            }           
        }

        public void checkList()
        {
            for(int i=0; i< ChromosomeList.Length; i++)
            {
                for(int j=0; j< ChromosomeList[i].Length; j++)
                {
                    int dura = 0;
                    for(int k=0; k< ChromosomeList[i][j].Length; k++)
                    {
                        dura += ChromosomeList[i][j].Note[k].duration;
                    }
                    if(dura != StaticRepo.ConfigRepository.TickPerOneBar)
                    {
                        throw new Exception("Duration length problem");
                    }
                }
            }
        }

        public void NextGeneration()
        {
            for (int i = 0; i < NumOfBar; i++)
            {
                NextGenerationBar(i);
            }
            StaticRepo.ScoreRepository.MakeNotesUsingGA();
            StaticRepo.ConfigRepository.Generation++;
            UpdateAnalytics();

            StaticRepo.ConfigRepository.BestFitness = 0;
            StaticRepo.ConfigRepository.AverageFitness = 0;

            for (int i = 0; i < NumOfBar; i++)
            {
                StaticRepo.ConfigRepository.BestFitness += StaticRepo.GARepository.Analytics[StaticRepo.ConfigRepository.Generation - 1].Best[0][i];
                StaticRepo.ConfigRepository.AverageFitness += StaticRepo.GARepository.Analytics[StaticRepo.ConfigRepository.Generation - 1].Average[0][i];
            }
        }

        public void NextGenerationBar(int barNumber)
        {
            var population = ChromosomeList[barNumber];
            double[] Fitnesses = (from p in population select p.Fitness).ToArray();
            double Best = Fitnesses.Max();
            double Worst = Fitnesses.Min();

            checkList();

            Chromosome[] NextGeneration = new Chromosome[Population];
            double[] RouletteWheelFitnesses = new double[Population];

            // 룰렛 휠 : (Ci - Cw) + ((Cb - Cw) / (k - 1))
            for (int i = 0; i < Population; i++)
            {
                RouletteWheelFitnesses[i] = (Fitnesses[i] - Worst) + (Best - Worst) / (StaticRepo.ConfigRepository.RouletteK - 1);
            }

            double SumOfFitness = RouletteWheelFitnesses.Sum();
            StaticRepo.TableRepository.ConvertChordTable();
            var elitepop = population.OrderByDescending(p => p.Fitness).ToArray();

            for (int j = 0; j < StaticRepo.ConfigRepository.Elitism; j++)
            {
                NextGeneration[j] = elitepop[j];
            }

            for (int j = StaticRepo.ConfigRepository.Elitism; j < Population; j++)
            {
                // 중복된거라도 좋으니 선택부터 한다
                Chromosome c1 = population[(RouletteWheelSelection(SumOfFitness, RouletteWheelFitnesses))];
                Chromosome c2 = population[(RouletteWheelSelection(SumOfFitness, RouletteWheelFitnesses))];
                
                NextGeneration[j] = CrossOver(c1, c2);
                
                NextGeneration[j].Mutate(StaticRepo.ConfigRepository.Mutation, 5);
                NextGeneration[j].MutateLegato(StaticRepo.ConfigRepository.Legatopercentage);
                NextGeneration[j].CalcFitness(StaticRepo.ScoreRepository.ChordListUsingGA[barNumber]);
            }
            
            ChromosomeList[barNumber] = NextGeneration;
        }

        public void UpdateAnalytics()
        {
            int NumEnum = Enum.GetNames(typeof(GA_AnalEnum)).Length;
            double[][] Best = new double[NumEnum][];
            double[][] Average = new double[NumEnum][];
            double[][] Worst = new double[NumEnum][];

            for (int i = 0; i < NumEnum; i++)
            {
                Best[i] = new double[NumOfBar];
                Average[i] = new double[NumOfBar];
                Worst[i] = new double[NumOfBar];
            }

            StaticRepo.GARepository.Add_Analytisc();

            for (int i = 0; i < NumOfBar; i++)
            {
                var population = ChromosomeList[i].ToList();

                // double[][] Fitnesses = (from p in population select p.Fitness_Detail).ToArray();

                double fmax = population.Max(c => c.Fitness);
                double fmin = population.Min(c => c.Fitness);
                double faver = population.Average(c => c.Fitness);

                Chromosome best = population.Find(p => p.Fitness == fmax);
                Chromosome worst = population.Find(p => p.Fitness == fmin);
                
                //double[][] FitnessesSwap = new double[NumEnum][];
                //for (int j = 0; j < NumEnum; j++)
                //{
                //    FitnessesSwap[j] = new double[Population];
                //    for (int k = 0; k < Population; k++)
                //    {
                //        FitnessesSwap[j][k] = Fitnesses[k][j];
                //    }
                //}
                
                for (int j = 0; j < NumEnum; j++)
                {
                    Best[j][i] = best.Fitness_Detail[j];
                    Worst[j][i] = worst.Fitness_Detail[j];
                    Average[j][i] = population.Average(p => p.Fitness_Detail[j]);

                    //Best[j][i] = FitnessesSwap[j].Max();
                    //Worst[j][i] = FitnessesSwap[j].Min();
                    //Average[j][i] = FitnessesSwap[j].Average();
                }

                StaticRepo.GARepository.Modify_Analytisc(StaticRepo.ConfigRepository.Generation, GA_AnalEnum.Fitness, Best, Average, Worst);
            }

            #region For Analytics

            List<GA_Analytics> Analytics = StaticRepo.GARepository.Analytics;

            StreamWriter sw = new StreamWriter("analytics.csv");

            sw.WriteLine("Generation, Best, Average, Worst");

            //sw.WriteLine("Generation, Bar Number, Type of Fitness, Best, Average, Worst");

            //for (int i = 0; i < StaticRepo.ConfigRepository.Generation; i++)
            //{
            //    for (int j = 0; j < NumEnum; j++)
            //    {
            //        for (int k = 0; k < NumOfBar; k++)
            //        {
            //            sw.WriteLine(Analytics[i].Generation + ", " +
            //                         (k + 1).ToString() + ", " +
            //                         Enum.GetNames(typeof(GA_AnalEnum))[j] + ", " +
            //                         Analytics[i].Best[j][k].ToString() + ", " +
            //                         Analytics[i].Average[j][k].ToString() + ", " +
            //                         Analytics[i].Worst[j][k].ToString()
            //                         );
            //        }
            //    }
            //}

            for (int i = 0; i < StaticRepo.ConfigRepository.Generation; i++)
            {
                
                for (int j = 0; j < 1; j++)
                {
                    double best = 0, average = 0, worst = 0;
                    for (int k = 0; k < NumOfBar; k++)
                    {
                        best += Analytics[i].Best[j][k];
                        average += Analytics[i].Average[j][k];
                        worst += Analytics[i].Worst[j][k];

                        //sw.WriteLine(Analytics[i].Generation + ", " +
                        //             (k + 1).ToString() + ", " +
                        //             Enum.GetNames(typeof(GA_AnalEnum))[j] + ", " +
                        //             Analytics[i].Best[j][k].ToString() + ", " +
                        //             Analytics[i].Average[j][k].ToString() + ", " +
                        //             Analytics[i].Worst[j][k].ToString()
                        //             );
                    }

                    sw.WriteLine(Analytics[i].Generation + ", " +
                         best.ToString() + ", " +
                         average.ToString() + ", " +
                         worst.ToString()
                         );
                }
            }

            sw.Close();
            #endregion

        }

        private double Minimum(double d1, double d2, double d3)
        {
            double minimun = d1;
            if (minimun > d2)
                minimun = d2;
            if (minimun > d3)
                minimun = d3;
            return minimun;
        }

        public Chromosome CrossOver(Chromosome c1, Chromosome c2)
        {
            #region 옛날 Crossover

            //#region Edit Distance Table 초기화. 어차피 채워질 테이블이라 굳이 0으로 다 할필요는 없다

            //for (int i = 0; i < StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            //{
            //    for (int j = 0; j < StaticRepo.ConfigRepository.MaxNumOfNote; j++)
            //    {
            //        EditDistanceTable[i][j] = 0;
            //    }
            //}

            //for (int i = 0; i <= c1.Length; i++)
            //{
            //    EditDistanceTable[i][0] = i;
            //}

            //for (int j = 0; j <= c2.Length; j++)
            //{
            //    EditDistanceTable[0][j] = j;
            //}

            //#endregion


            //#region Edit Distance Table 채워넣기

            //for (int i = 1; i <= c1.Length; i++)
            //{
            //    for (int j = 1; j <= c2.Length; j++)
            //    {
            //        // Exact Match
            //        if ((c1.Note[i - 1].pitch == c2.Note[j - 1].pitch) && (c1.Note[i - 1].duration == c2.Note[j - 1].duration))
            //        {
            //            EditDistanceTable[i][j] = EditDistanceTable[i - 1][j - 1];
            //        }
            //        // Pitch Match
            //        else if ((c1.Note[i - 1].pitch == c2.Note[j - 1].pitch) && (c1.Note[i - 1].duration != c2.Note[j - 1].duration))
            //        {
            //            EditDistanceTable[i][j] = EditDistanceTable[i - 1][j - 1] + 0.4;
            //        }
            //        // Replace, Add, Delete
            //        else
            //        {
            //            EditDistanceTable[i][j] = Minimum(EditDistanceTable[i - 1][j - 1], EditDistanceTable[i - 1][j], EditDistanceTable[i][j - 1]) + 1;
            //        }
            //    }
            //}

            ////if (EditDistanceTable[c1.Length][c2.Length] == 0)
            ////{
            ////    return c1;
            ////}

            //#endregion

            #region 디버깅용. 디버그 모드에서 이것들을 실행한 후 dt 를 Visualizer 를 이용하여 볼 수 있다

            //DataTable dt = new DataTable();

            //for (int i = 0; i <= c1.Length + 2; i++)
            //{
            //    dt.Columns.Add();
            //}

            //for (int i = 0; i <= c2.Length + 2; i++)
            //{
            //    DataRow dr = dt.NewRow();
            //    for (int j = 0; j <= c1.Length + 2; j++)
            //    {
            //        dr[j] = 0;
            //    }
            //    dt.Rows.Add(dr);
            //}

            //for (int i = 0; i <= c2.Length + 1; i++)
            //{
            //    for (int j = 0; j <= c1.Length + 1; j++)
            //    {
            //        if (i == 0 && j == 0)
            //        {
            //            dt.Rows[i][j] = "";
            //        }
            //        else if (i == 0)
            //        {
            //            dt.Rows[i][j + 1] = c1.Note[j - 1].pitch.ToString() + ", " + (c1.Note[j - 1].duration / 240).ToString();
            //        }
            //        else if (j == 0)
            //        {
            //            dt.Rows[i + 1][j] = c2.Note[i - 1].pitch.ToString() + ", " + (c2.Note[i - 1].duration / 240).ToString();
            //        }
            //        else
            //        {
            //            dt.Rows[i][j] = EditDistanceTable[j - 1][i - 1];
            //        }
            //    }
            //}

            #endregion

            //#region Edit Distance Table 을 역추적 하면서 Score Table 에 값 넣기

            //int c1_index = c1.Length;
            //int c2_index = c2.Length;
            //ScoreTable = new List<NoteScore>();
            //double min = 0;

            //while (!(c1_index == 0 && c2_index == 0))
            //{
            //    if (c1_index == 0)
            //    {
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c2.Note[c2_index - 1].duration,
            //                    pitch = c2.Note[c2_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c2_index--;
            //        }
            //    }
            //    else if (c2_index == 0)
            //    {
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c1.Note[c1_index - 1].duration,
            //                    pitch = c1.Note[c1_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c1_index--;
            //        }
            //    }
            //    else
            //    {
            //        min = Minimum(EditDistanceTable[c1_index - 1][c2_index - 1], EditDistanceTable[c1_index - 1][c2_index], EditDistanceTable[c1_index][c2_index - 1]);

            //        if (min == EditDistanceTable[c1_index][c2_index]) // 변경 없음
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c1.Note[c1_index - 1].duration,
            //                    pitch = c1.Note[c1_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c1_index--;
            //            c2_index--;
            //        }
            //        else if (min == EditDistanceTable[c1_index - 1][c2_index - 1]) // 변경함
            //        {
            //            if (c1.Note[c1_index - 1].pitch == c2.Note[c2_index - 1].pitch) // 만일 Pitch 가 같다면 선택되게 한다
            //            {
            //                if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 3) == 1)
            //                {
            //                    ScoreTable.Add(new NoteScore
            //                    {
            //                        note = new GeneNote
            //                        {
            //                            duration = c1.Note[c1_index - 1].duration,
            //                            pitch = c1.Note[c1_index - 1].pitch
            //                        },
            //                        selected = false
            //                    });
            //                }
            //                else
            //                {
            //                    ScoreTable.Add(new NoteScore
            //                    {
            //                        note = new GeneNote
            //                        {
            //                            duration = c2.Note[c2_index - 1].duration,
            //                            pitch = c2.Note[c2_index - 1].pitch
            //                        },
            //                        selected = false
            //                    });
            //                }
            //            }
            //            else
            //            {
            //                ScoreTable.Add(new NoteScore
            //                {
            //                    note = new GeneNote
            //                    {
            //                        duration = c1.Note[c1_index - 1].duration,
            //                        pitch = c1.Note[c1_index - 1].pitch
            //                    },
            //                    selected = false
            //                });

            //                ScoreTable.Add(new NoteScore
            //                {
            //                    note = new GeneNote
            //                    {
            //                        duration = c2.Note[c2_index - 1].duration,
            //                        pitch = c2.Note[c2_index - 1].pitch
            //                    },
            //                    selected = false
            //                });
            //            }

            //            c1_index--;
            //            c2_index--;
            //        }
            //        else if (min == EditDistanceTable[c1_index - 1][c2_index]) // c1[c1_index] 가 제거됨
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c1.Note[c1_index - 1].duration,
            //                    pitch = c1.Note[c1_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c1_index--;
            //        }
            //        else if (min == EditDistanceTable[c1_index][c2_index - 1]) // c2[c2_index] 가 추가됨
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c2.Note[c2_index - 1].duration,
            //                    pitch = c2.Note[c2_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c2_index--;
            //        }
            //    }
            //}

            //#endregion

            //#region 새로운 Chromosome 을 만들기

            //// Selected 가 true 인 노트들의 duration 합이 16이 될 때 까지 점수가 높은 note 의 selected 를 true 로 만들어준다
            //while (ScoreTable.FindAll(note => note.selected == true).Sum(score => score.note.duration) < (16 * 240))
            //{
            //    var randomselect = ScoreTable.FindAll(note => note.selected == false)
            //        [(int)(StaticRepo.ConfigRepository.GlobalRandom.NextDouble() * ScoreTable.FindAll(note => note.selected == false).Count)];

            //    randomselect.selected = true;

            //    if (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) > (16 * 240))
            //    {
            //        randomselect.note.duration -= (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) - (16 * 240));
            //    }
            //}

            //Chromosome NextC = new Chromosome(c1.BarNumber);

            //int length = 0;
            //for (int i = 0; i < ScoreTable.Count; i++)
            //{
            //    if (ScoreTable[ScoreTable.Count - i - 1].selected == true)
            //    {
            //        NextC.Note[length] = ScoreTable[ScoreTable.Count - i - 1].note;
            //        length++;
            //    }
            //}

            //NextC.Length = length;

            //#endregion


            #endregion
            
            #region Crossover ver 1 = 16분음표 기반 Rest 없음

            //double[] BetweenTable = StaticRepo.TableRepository.SavedTable.SequenceTable.betweenNotesPitchDevided;

            //#region Edit Distance Table 초기화. 어차피 채워질 테이블이라 굳이 0으로 다 할필요는 없다

            //for (int i = 0; i < StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            //{
            //    for (int j = 0; j < StaticRepo.ConfigRepository.MaxNumOfNote; j++)
            //    {
            //        EditDistanceTable[i][j] = 0;
            //    }
            //}

            //for (int i = 0; i <= c1.Length; i++)
            //{
            //    EditDistanceTable[i][0] = (2 - BetweenTable[13])*i;
            //}

            //for (int j = 0; j <= c2.Length; j++)
            //{
            //    EditDistanceTable[0][j] = (2 - BetweenTable[13])*j;
            //}

            //#endregion

            //#region Edit Distance Table 채워넣기
            
            //for (int i = 1; i <= c1.Length; i++)
            //{
            //    for (int j = 1; j <= c2.Length; j++)
            //    {
            //        // Exact Match
            //        if ((c1.Note[i - 1].pitch == c2.Note[j - 1].pitch) && (c1.Note[i - 1].duration == c2.Note[j - 1].duration))
            //        {
            //            EditDistanceTable[i][j] = EditDistanceTable[i - 1][j - 1];
            //        }
            //        // Pitch Match
            //        else if ((c1.Note[i - 1].pitch == c2.Note[j - 1].pitch) && (c1.Note[i - 1].duration != c2.Note[j - 1].duration))
            //        {
            //            EditDistanceTable[i][j] = EditDistanceTable[i - 1][j - 1] + (1 - BetweenTable[0]);
            //        }
            //        // Replace, Add, Delete
            //        else
            //        {
            //            int between = Math.Abs((c1.Note[i - 1].pitch - c2.Note[j - 1].pitch));
            //            if (between < 13)
            //            {
            //                EditDistanceTable[i][j] = Minimum(EditDistanceTable[i - 1][j - 1], EditDistanceTable[i - 1][j], EditDistanceTable[i][j - 1]) + (2 - BetweenTable[between]);
            //            }
            //            else
            //            {
            //                EditDistanceTable[i][j] = Minimum(EditDistanceTable[i - 1][j - 1], EditDistanceTable[i - 1][j], EditDistanceTable[i][j - 1]) + (2 - BetweenTable[13]);
            //            }
            //        }
            //    }
            //}

            //#endregion

            //#region 디버깅용. 디버그 모드에서 이것들을 실행한 후 dt 를 Visualizer 를 이용하여 볼 수 있다

            ////DataTable dt = new DataTable();

            ////for (int i = 0; i <= c1.Length + 2; i++)
            ////{
            ////    dt.Columns.Add();
            ////}

            ////for (int i = 0; i <= c2.Length + 2; i++)
            ////{
            ////    DataRow dr = dt.NewRow();
            ////    for (int j = 0; j <= c1.Length + 2; j++)
            ////    {
            ////        dr[j] = 0;
            ////    }
            ////    dt.Rows.Add(dr);
            ////}

            ////for (int i = 0; i <= c2.Length + 1; i++)
            ////{
            ////    for (int j = 0; j <= c1.Length + 1; j++)
            ////    {
            ////        if (i == 0 && j == 0)
            ////        {
            ////            dt.Rows[i][j] = "";
            ////        }
            ////        else if (i == 0)
            ////        {
            ////            dt.Rows[i][j + 1] = c1.Note[j - 1].pitch.ToString() + ", " + (c1.Note[j - 1].duration / 240).ToString();
            ////        }
            ////        else if (j == 0)
            ////        {
            ////            dt.Rows[i + 1][j] = c2.Note[i - 1].pitch.ToString() + ", " + (c2.Note[i - 1].duration / 240).ToString();
            ////        }
            ////        else
            ////        {
            ////            dt.Rows[i][j] = Math.Round(EditDistanceTable[j - 1][i - 1], 2);
            ////        }
            ////    }
            ////}

            //#endregion

            //#region Edit Distance Table 을 역추적 하면서 Score Table 에 값 넣기

            //int c1_index = c1.Length;
            //int c2_index = c2.Length;
            //ScoreTable = new List<NoteScore>();
            //double min = 0;
            
            //while (!(c1_index == 0 && c2_index == 0))
            //{
            //    if (c1_index == 0)
            //    {
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c2.Note[c2_index - 1].duration,
            //                    pitch = c2.Note[c2_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c2_index--;
            //        }
            //    }
            //    else if (c2_index == 0)
            //    {
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c1.Note[c1_index - 1].duration,
            //                    pitch = c1.Note[c1_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c1_index--;
            //        }
            //    }
            //    else
            //    {
            //        min = Minimum(EditDistanceTable[c1_index - 1][c2_index - 1], EditDistanceTable[c1_index - 1][c2_index], EditDistanceTable[c1_index][c2_index - 1]);

            //        if (min == EditDistanceTable[c1_index][c2_index]) // 변경 없음
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c1.Note[c1_index - 1].duration,
            //                    pitch = c1.Note[c1_index - 1].pitch
            //                },
            //                selected = true
            //            });

            //            c1_index--;
            //            c2_index--;
            //        }
            //        else if (min == EditDistanceTable[c1_index - 1][c2_index - 1]) // 변경함
            //        {
            //            if (c1.Note[c1_index - 1].pitch == c2.Note[c2_index - 1].pitch) // 만일 Pitch 가 같다면 선택되게 한다
            //            {
            //                if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 3) == 1) // Duration 이 같은 것은 똑같다고 보고 랜덤하게 선택
            //                {
            //                    ScoreTable.Add(new NoteScore
            //                    {
            //                        note = new GeneNote
            //                        {
            //                            duration = c1.Note[c1_index - 1].duration,
            //                            pitch = c1.Note[c1_index - 1].pitch
            //                        },
            //                        selected = true
            //                    });
            //                }
            //                else
            //                {
            //                    ScoreTable.Add(new NoteScore
            //                    {
            //                        note = new GeneNote
            //                        {
            //                            duration = c2.Note[c2_index - 1].duration,
            //                            pitch = c2.Note[c2_index - 1].pitch
            //                        },
            //                        selected = true
            //                    });
            //                }
            //            }
            //            else // 만일 아니면 두개 다 올려 두고 선택은 되지 않게 한다
            //            {
            //                ScoreTable.Add(new NoteScore
            //                {
            //                    note = new GeneNote
            //                    {
            //                        duration = c1.Note[c1_index - 1].duration,
            //                        pitch = c1.Note[c1_index - 1].pitch
            //                    },
            //                    selected = false
            //                });

            //                ScoreTable.Add(new NoteScore
            //                {
            //                    note = new GeneNote
            //                    {
            //                        duration = c2.Note[c2_index - 1].duration,
            //                        pitch = c2.Note[c2_index - 1].pitch
            //                    },
            //                    selected = false
            //                });
            //            }

            //            c1_index--;
            //            c2_index--;
            //        }
            //        else if (min == EditDistanceTable[c1_index - 1][c2_index]) // c1[c1_index] 가 제거됨
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c1.Note[c1_index - 1].duration,
            //                    pitch = c1.Note[c1_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c1_index--;
            //        }
            //        else if (min == EditDistanceTable[c1_index][c2_index - 1]) // c2[c2_index] 가 추가됨
            //        {
            //            ScoreTable.Add(new NoteScore
            //            {
            //                note = new GeneNote
            //                {
            //                    duration = c2.Note[c2_index - 1].duration,
            //                    pitch = c2.Note[c2_index - 1].pitch
            //                },
            //                selected = false
            //            });

            //            c2_index--;
            //        }
            //    }
            //}

            //#endregion

            //#region 새로운 Chromosome 을 만들기

            //#region Repair : 랜덤적으로 선택하다보면 총 Length 가 낮을 수도 있다. 하나씩 랜덤 선택해서 올려준다

            //while (ScoreTable.Sum(n => n.note.duration) < StaticRepo.ConfigRepository.TickPerOneBar)
            //{
            //    ScoreTable[StaticRepo.ConfigRepository.GlobalRandom.Next(0, ScoreTable.Count)].note.duration += StaticRepo.ConfigRepository.TickPerMinNote;
            //}
            
            //#endregion

            //// 일단 거꾸로 들어갔기 때문에 swap 한다
            //NoteScore temp;
            //for(int i=0; i< ScoreTable.Count/2; i++)
            //{
            //    temp = ScoreTable[i];
            //    ScoreTable[i] = ScoreTable[ScoreTable.Count - i - 1];
            //    ScoreTable[ScoreTable.Count - i - 1] = temp;
            //}

            //double[] ScoreOfScoreTable = new double[ScoreTable.Count];

            //#region Repair2 : true 체크 된 것들이 3840을 넘을수도 있다. 같은 방식으로 리페어링 한다

            //while (ScoreTable.FindAll(note => note.selected == true).Sum(n => n.note.duration) > StaticRepo.ConfigRepository.TickPerOneBar)
            //{
            //    var v = ScoreTable[StaticRepo.ConfigRepository.GlobalRandom.Next(0, ScoreTable.Count)].note;
            //    if(v.duration != StaticRepo.ConfigRepository.TickPerMinNote)
            //    {
            //        v.duration -= StaticRepo.ConfigRepository.TickPerMinNote;
            //    }
            //}

            //#endregion

            //// Selected 가 true 인 노트들의 duration 합이 16이 될 때 까지 점수가 높은 note 의 selected 를 true 로 만들어준다

            //while (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) < StaticRepo.ConfigRepository.TickPerOneBar)
            //{
            //    // selected == true 인 노트들과의 노트차이를 이용하여 false 인 노트들의 score 를 구한다. 

            //    for (int i = 0; i < ScoreTable.Count; i++)
            //    {
            //        ScoreOfScoreTable[i] = 0;
            //    }

            //    for (int i = 0; i < ScoreTable.Count; i++)
            //    {
            //        // 자신의 index 가 true 라면 왼쪽에는 1을 올리고 오른쪽에는 2를 올리고 자신은 -2가 된다
            //        // 결과적으로 자신이 0이면 양 옆에 false, 음수면 자신이 true, 
            //        // 1이면 오른쪽에만 true 가 있다, 2이면 왼쪽에만 true 가 있다, 3이면 양쪽에 true 가 있다
            //        if (ScoreTable[i].selected == true)
            //        {
            //            if (i != ScoreTable.Count - 1)
            //            {
            //                ScoreOfScoreTable[i + 1] += 2;
            //            }

            //            ScoreOfScoreTable[i] = -2;

            //            if (i != 0)
            //            {
            //                ScoreOfScoreTable[i - 1] += 1;
            //            }
            //        }
            //    }

            //    for (int i = 0; i < ScoreTable.Count; i++)
            //    {
            //        if (ScoreOfScoreTable[i] == 0)
            //        {
            //            ScoreOfScoreTable[i] = 0.2;
            //        }
            //        else if (ScoreOfScoreTable[i] == 1)
            //        {
            //            double between = Math.Abs(ScoreTable[i].note.pitch - ScoreTable[i + 1].note.pitch);
            //            if (between > 13)
            //                ScoreOfScoreTable[i] = BetweenTable[13];
            //            else
            //            {
            //                ScoreOfScoreTable[i] = BetweenTable[(int)between];
            //            }
            //        }
            //        else if (ScoreOfScoreTable[i] == 2)
            //        {
            //            double between = Math.Abs(ScoreTable[i].note.pitch - ScoreTable[i - 1].note.pitch);
            //            if (between > 13)
            //                ScoreOfScoreTable[i] = BetweenTable[13];
            //            else
            //            {
            //                ScoreOfScoreTable[i] = BetweenTable[(int)between];
            //            }
            //        }
            //        else if (ScoreOfScoreTable[i] == 3) // 양쪽이 같이 있으면 중간값으로 한다
            //        {
            //            double between1 = Math.Abs(ScoreTable[i].note.pitch - ScoreTable[i - 1].note.pitch);
            //            double between2 = Math.Abs(ScoreTable[i].note.pitch - ScoreTable[i + 1].note.pitch);

            //            if (between1 > 13)
            //                ScoreOfScoreTable[i] += BetweenTable[13];
            //            else
            //            {
            //                ScoreOfScoreTable[i] = BetweenTable[(int)between1];
            //            }

            //            if (between2 > 13)
            //                ScoreOfScoreTable[i] += BetweenTable[13];
            //            else
            //            {
            //                ScoreOfScoreTable[i] = BetweenTable[(int)between2];
            //            }

            //            ScoreOfScoreTable[i] /= 2;
            //        }
            //    }

            //    List<NoteScore> tempEnum = new List<NoteScore>();
            //    double max = ScoreOfScoreTable.Max();
            //    for (int i = 0; i < ScoreOfScoreTable.Length; i++)
            //    {
            //        if(ScoreOfScoreTable[i] == max)
            //        {
            //            tempEnum.Add(ScoreTable[i]);
            //        }
            //    }
                
            //    int selectIndex = StaticRepo.ConfigRepository.GlobalRandom.Next(0, tempEnum.Count());
            //    tempEnum[selectIndex].selected = true;

            //    // 음표의 duration 이 초과하였을 때
            //    if (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) > StaticRepo.ConfigRepository.TickPerOneBar)
            //    {
            //        tempEnum[selectIndex].note.duration -= (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) - StaticRepo.ConfigRepository.TickPerOneBar);

            //        #region 옛날
            //        //// 만약에 선택한 음표들이 최대치(16개를) 넘지 않는다면
            //        //if (ScoreTable.ToList().FindAll(note => note.selected == true).Count != StaticRepo.ConfigRepository.MaxNumOfNote)
            //        //{
            //        //    // 만약에 초과한 음표를 빼서 왼쪽 음표의 duration 에 추가 된 것이 점수가 더 높다면
            //        //    if (StaticRepo.TableRepository.SavedTable.SequenceTable.theNumberOfNotesDevided[ScoreTable.ToList().FindAll(note => note.selected == true).Count - 1] >
            //        //    StaticRepo.TableRepository.SavedTable.SequenceTable.theNumberOfNotesDevided[ScoreTable.ToList().FindAll(note => note.selected == true).Count])
            //        //    {

            //        //    }
            //        //        randomselect.note.duration -= (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) - (16 * 240));
            //        //}
            //        //else
            //        //{

            //        //}
            //        #endregion
            //    }

            //}

            //Chromosome NextC = new Chromosome(c1.BarNumber);

            //int length = 0;
            //for (int i = 0; i < ScoreTable.Count; i++)
            //{
            //    if (ScoreTable[i].selected == true)
            //    {
            //        NextC.Note[length].pitch = ScoreTable[i].note.pitch;
            //        NextC.Note[length].duration = ScoreTable[i].note.duration;
            //        length++;
            //    }
            //}
            
            //NextC.Length = length;

            //if(NextC.Note.Sum(n=>n.duration) != StaticRepo.ConfigRepository.TickPerOneBar)
            //{
            //    throw new Exception("Crossover : Sum > TickPerOneBar");
            //}

            //#endregion

            #endregion

            #region Crossover ver 2 = 8븐 셋잇단음표 기반 Rest 있음

            double[] BetweenTable = StaticRepo.TableRepository.SavedTable.SequenceTable.betweenNotesPitchDevided;

            #region Edit Distance Table 초기화. 어차피 채워질 테이블이라 굳이 0으로 다 할필요는 없다

            for (int i = 0; i < StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            {
                for (int j = 0; j < StaticRepo.ConfigRepository.MaxNumOfNote; j++)
                {
                    EditDistanceTable[i][j] = 0;
                }
            }

            for (int i = 0; i <= c1.Length; i++)
            {
                EditDistanceTable[i][0] = 2 * i;
            }

            for (int j = 0; j <= c2.Length; j++)
            {
                EditDistanceTable[0][j] = 2 * j;
            }

            #endregion

            #region Edit Distance Table 채워넣기

            for (int i = 1; i <= c1.Length; i++)
            {
                for (int j = 1; j <= c2.Length; j++)
                {
                    // Exact Match
                    if ((c1.Note[i - 1].pitch == c2.Note[j - 1].pitch) && (c1.Note[i - 1].duration == c2.Note[j - 1].duration))
                    {
                        EditDistanceTable[i][j] = EditDistanceTable[i - 1][j - 1];
                    }
                    // Pitch Match
                    else if ((c1.Note[i - 1].pitch == c2.Note[j - 1].pitch) && (c1.Note[i - 1].duration != c2.Note[j - 1].duration))
                    {
                        EditDistanceTable[i][j] = Minimum(EditDistanceTable[i - 1][j - 1], EditDistanceTable[i - 1][j], EditDistanceTable[i][j - 1]) + 1;
                    }
                    // Replace, Add, Delete
                    else
                    {
                        EditDistanceTable[i][j] = Minimum(EditDistanceTable[i - 1][j - 1], EditDistanceTable[i - 1][j], EditDistanceTable[i][j - 1]) + 2;
                    }
                }
            }

            #endregion

            #region 디버깅용. 디버그 모드에서 이것들을 실행한 후 dt 를 Visualizer 를 이용하여 볼 수 있다

            //DataTable dt = new DataTable();

            //for (int i = 0; i <= c1.Length + 2; i++)
            //{
            //    dt.Columns.Add();
            //}

            //for (int i = 0; i <= c2.Length + 2; i++)
            //{
            //    DataRow dr = dt.NewRow();
            //    for (int j = 0; j <= c1.Length + 2; j++)
            //    {
            //        dr[j] = 0;
            //    }
            //    dt.Rows.Add(dr);
            //}

            //for (int i = 0; i <= c2.Length + 1; i++)
            //{
            //    for (int j = 0; j <= c1.Length + 1; j++)
            //    {
            //        if (i == 0 && j == 0)
            //        {
            //            dt.Rows[i][j] = "";
            //        }
            //        else if (i == 0)
            //        {
            //            dt.Rows[i][j + 1] = c1.Note[j - 1].pitch.ToString() + ", " + (c1.Note[j - 1].duration / 320).ToString();
            //        }
            //        else if (j == 0)
            //        {
            //            dt.Rows[i + 1][j] = c2.Note[i - 1].pitch.ToString() + ", " + (c2.Note[i - 1].duration / 320).ToString();
            //        }
            //        else
            //        {
            //            dt.Rows[i][j] = Math.Round(EditDistanceTable[j - 1][i - 1], 2);
            //        }
            //    }
            //}

            #endregion

            #region Edit Distance Table 을 역추적 하면서 Score Table 에 값 넣기

            int c1_index = c1.Length;
            int c2_index = c2.Length;
            ScoreTable = new List<NoteScore>();
            double min = 0;

            while (!(c1_index == 0 && c2_index == 0))
            {
                if (c1_index == 0)
                {
                    {
                        ScoreTable.Add(new NoteScore
                        {
                            note = new GeneNote
                            {
                                duration = c2.Note[c2_index - 1].duration,
                                pitch = c2.Note[c2_index - 1].pitch
                            },
                            selected = false
                        });

                        c2_index--;
                    }
                }
                else if (c2_index == 0)
                {
                    {
                        ScoreTable.Add(new NoteScore
                        {
                            note = new GeneNote
                            {
                                duration = c1.Note[c1_index - 1].duration,
                                pitch = c1.Note[c1_index - 1].pitch
                            },
                            selected = false
                        });

                        c1_index--;
                    }
                }
                else
                {
                    min = Minimum(EditDistanceTable[c1_index - 1][c2_index - 1], EditDistanceTable[c1_index - 1][c2_index], EditDistanceTable[c1_index][c2_index - 1]);

                    if (min == EditDistanceTable[c1_index][c2_index]) // 변경 없음
                    {
                        ScoreTable.Add(new NoteScore
                        {
                            note = new GeneNote
                            {
                                duration = c1.Note[c1_index - 1].duration,
                                pitch = c1.Note[c1_index - 1].pitch
                            },
                            selected = true
                        });

                        c1_index--;
                        c2_index--;
                    }
                    else if (min == EditDistanceTable[c1_index - 1][c2_index - 1]) // 변경함
                    {
                        if (c1.Note[c1_index - 1].pitch == c2.Note[c2_index - 1].pitch) // 만일 Pitch 가 같다면 선택되게 한다
                        {
                            if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 3) == 1) // Duration 이 같은 것은 똑같다고 보고 랜덤하게 선택
                            {
                                ScoreTable.Add(new NoteScore
                                {
                                    note = new GeneNote
                                    {
                                        duration = c1.Note[c1_index - 1].duration,
                                        pitch = c1.Note[c1_index - 1].pitch
                                    },
                                    selected = true
                                });
                            }
                            else
                            {
                                ScoreTable.Add(new NoteScore
                                {
                                    note = new GeneNote
                                    {
                                        duration = c2.Note[c2_index - 1].duration,
                                        pitch = c2.Note[c2_index - 1].pitch
                                    },
                                    selected = true
                                });
                            }
                        }
                        else // 만일 아니면 두개 다 올려 두고 선택은 되지 않게 한다
                        {
                            ScoreTable.Add(new NoteScore
                            {
                                note = new GeneNote
                                {
                                    duration = c1.Note[c1_index - 1].duration,
                                    pitch = c1.Note[c1_index - 1].pitch
                                },
                                selected = false
                            });

                            ScoreTable.Add(new NoteScore
                            {
                                note = new GeneNote
                                {
                                    duration = c2.Note[c2_index - 1].duration,
                                    pitch = c2.Note[c2_index - 1].pitch
                                },
                                selected = false
                            });
                        }

                        c1_index--;
                        c2_index--;
                    }
                    else if (min == EditDistanceTable[c1_index - 1][c2_index]) // c1[c1_index] 가 제거됨
                    {
                        ScoreTable.Add(new NoteScore
                        {
                            note = new GeneNote
                            {
                                duration = c1.Note[c1_index - 1].duration,
                                pitch = c1.Note[c1_index - 1].pitch
                            },
                            selected = false
                        });

                        c1_index--;
                    }
                    else if (min == EditDistanceTable[c1_index][c2_index - 1]) // c2[c2_index] 가 추가됨
                    {
                        ScoreTable.Add(new NoteScore
                        {
                            note = new GeneNote
                            {
                                duration = c2.Note[c2_index - 1].duration,
                                pitch = c2.Note[c2_index - 1].pitch
                            },
                            selected = false
                        });

                        c2_index--;
                    }
                }
            }

            #endregion

            #region 새로운 Chromosome 을 만들기

            #region Repair : 랜덤적으로 선택하다보면 총 Length 가 낮을 수도 있다. 하나씩 랜덤 선택해서 올려준다

            while (ScoreTable.Sum(n => n.note.duration) < StaticRepo.ConfigRepository.TickPerOneBar)
            {
                ScoreTable[StaticRepo.ConfigRepository.GlobalRandom.Next(0, ScoreTable.Count)].note.duration += StaticRepo.ConfigRepository.TickPerMinNote;
            }

            #endregion

            // 일단 거꾸로 들어갔기 때문에 swap 한다
            NoteScore temp;
            for (int i = 0; i < ScoreTable.Count / 2; i++)
            {
                temp = ScoreTable[i];
                ScoreTable[i] = ScoreTable[ScoreTable.Count - i - 1];
                ScoreTable[ScoreTable.Count - i - 1] = temp;
            }

            double[] ScoreOfScoreTable = new double[ScoreTable.Count];

            #region Repair2 : true 체크 된 것들이 3840을 넘을수도 있다. 같은 방식으로 리페어링 한다

            while (ScoreTable.FindAll(note => note.selected == true).Sum(n => n.note.duration) > StaticRepo.ConfigRepository.TickPerOneBar)
            {
                var v = ScoreTable[StaticRepo.ConfigRepository.GlobalRandom.Next(0, ScoreTable.Count)].note;
                if (v.duration != StaticRepo.ConfigRepository.TickPerMinNote)
                {
                    v.duration -= StaticRepo.ConfigRepository.TickPerMinNote;
                }
            }

            #endregion

            // Selected 가 true 인 노트들의 duration 합이 12가 될 때 까지 점수가 높은 note 의 selected 를 true 로 만들어준다

            while (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) < StaticRepo.ConfigRepository.TickPerOneBar)
            {
                int targetIndex = StaticRepo.ConfigRepository.GlobalRandom.Next(0, ScoreTable.Count());
                ScoreTable[targetIndex].selected = true;

                // 음표의 duration 이 초과하였을 때
                while (ScoreTable.ToList().FindAll(note => note.selected == true).Sum(score => score.note.duration) > StaticRepo.ConfigRepository.TickPerOneBar)
                {
                    ScoreTable[targetIndex].note.duration -= StaticRepo.ConfigRepository.TickPerMinNote;
                }
            }

            Chromosome NextC = new Chromosome(c1.BarNumber);

            int length = 0;
            for (int i = 0; i < ScoreTable.Count; i++)
            {
                if (ScoreTable[i].selected == true)
                {
                    NextC.Note[length].pitch = ScoreTable[i].note.pitch;
                    NextC.Note[length].duration = ScoreTable[i].note.duration;
                    length++;
                }
            }

            NextC.Length = length;

            if (NextC.Note.Sum(n => n.duration) != StaticRepo.ConfigRepository.TickPerOneBar)
            {
                throw new Exception("Crossover : Sum > TickPerOneBar");
            }

            #endregion

            #endregion


            return NextC;
        }
        
        public int RouletteWheelSelection(double sumOfFitness, double[] fitnesses)
        {
            double point = StaticRepo.ConfigRepository.GlobalRandom.NextDouble() * sumOfFitness;
            double sum = 0;
            for(int i=0; i<Population; i++)
            {
                sum += fitnesses[i];
                if (point <= sum)
                    return i;
            }

            return -1;
        }
    }
}
